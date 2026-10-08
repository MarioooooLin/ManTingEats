using ManTingEats.Models;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static ManTingEats.Tests.OrderControllerTestHost;

namespace ManTingEats.Tests;

public sealed class OrderControllerTests : IDisposable
{
    private readonly OrderControllerTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private static AddOrderItemCommand AddCommand(int menuItemId, int quantity = 1, int? spice = null, params (int Id, int Qty)[] addOns) =>
        new(menuItemId, quantity, null, spice,
            addOns.Length == 0 ? null : addOns.Select(a => new AddOnSelectionCommand(a.Id, a.Qty)).ToList());

    // ── 建立訂單 ────────────────────────────────────────────────

    [Fact]
    public async Task Create_DineInWithoutTable_ReturnsFormWithError()
    {
        var controller = _host.CreateController();

        var result = await controller.Create(new CreateOrderCommand(OrderChannel.DineIn, "  "));

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_SameDay_DailyNumberIncrements()
    {
        await _host.CreateController().Create(new CreateOrderCommand(OrderChannel.Takeout, null));
        await _host.CreateController().Create(new CreateOrderCommand(OrderChannel.Takeout, null));

        using var db = _host.CreateDbContext();
        Assert.Equal([1, 2], db.Orders.OrderBy(o => o.Id).Select(o => o.DailyNumber).ToList());
    }

    [Fact]
    public async Task Create_DuplicateOpenTable_AsksForConfirmationFirst()
    {
        await _host.CreateController().Create(new CreateOrderCommand(OrderChannel.DineIn, "A1"));

        // 桌號前後空白會被去除，仍視為同一桌
        var controller = _host.CreateController();
        var result = await controller.Create(new CreateOrderCommand(OrderChannel.DineIn, "A1 "));
        Assert.IsType<ViewResult>(result);
        Assert.NotNull(controller.ViewBag.DuplicateTableOrder);

        await _host.CreateController().Create(new CreateOrderCommand(OrderChannel.DineIn, "A1"), confirmDuplicateTable: true);
        using var db = _host.CreateDbContext();
        Assert.Equal(2, db.Orders.Count(o => o.TableNumber == "A1"));
    }

    // ── 加點與金額計算 ──────────────────────────────────────────

    [Fact]
    public async Task AddItem_SimpleItem_TotalIsPriceTimesQuantity()
    {
        var orderId = _host.SeedOrder();

        await _host.CreateController().AddItem(orderId, AddCommand(SimpleItemId, quantity: 3));

        var order = _host.LoadOrder(orderId);
        var item = Assert.Single(order.Items);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(100m, item.UnitPrice);
        Assert.Null(item.SpiceLevel);          // 不可調辣度的品項不記錄辣度
        Assert.False(item.IsPrinted);
        Assert.Equal(300m, order.TotalAmount);
    }

    [Fact]
    public async Task AddItem_CustomItem_SingleServingAddOnsPriced()
    {
        var orderId = _host.SeedOrder();

        await _host.CreateController().AddItem(orderId, AddCommand(CustomItemId, quantity: 1, spice: 3, (EggAddOnId, 2)));

        var order = _host.LoadOrder(orderId);
        var item = Assert.Single(order.Items);
        Assert.Equal(1, item.Quantity);
        Assert.Equal(3, item.SpiceLevel);
        var addOn = Assert.Single(item.AddOns);
        Assert.Equal("加蛋", addOn.AddOnName);
        Assert.Equal(10m, addOn.UnitPrice);
        Assert.Equal(140m, order.TotalAmount);  // 120 + 10 x 2
    }

    [Fact]
    public async Task AddItem_CustomItemMultipleServings_AddOnsAppliedToEachServing()
    {
        var orderId = _host.SeedOrder();

        // v10：客製化品項可點多份，加料數量為每份的量，存成同一行
        await _host.CreateController().AddItem(orderId, AddCommand(CustomItemId, quantity: 3, spice: 2, (EggAddOnId, 2)));

        var order = _host.LoadOrder(orderId);
        var item = Assert.Single(order.Items);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(2, item.SpiceLevel);
        Assert.Equal(2, Assert.Single(item.AddOns).Quantity);   // 存每份的量，不預先乘份數
        Assert.Equal(60m, item.AddOnsTotal);                    // 10 x 2 顆 x 3 份
        Assert.Equal(420m, order.TotalAmount);                  // (120 + 10 x 2) x 3
    }

    [Fact]
    public async Task RemoveItem_CustomItemMultipleServings_RecalculatesTotalWithAddOns()
    {
        var orderId = _host.SeedOrder();
        await _host.CreateController().AddItem(orderId, AddCommand(CustomItemId, quantity: 2, spice: 1, (EggAddOnId, 1)));
        await _host.CreateController().AddItem(orderId, AddCommand(SimpleItemId, quantity: 1));
        Assert.Equal(360m, _host.LoadOrder(orderId).TotalAmount);  // (120 + 10) x 2 + 100

        var simpleId = _host.LoadOrder(orderId).Items.Single(i => i.MenuItemId == SimpleItemId).Id;
        await _host.CreateController().RemoveItem(orderId, simpleId);

        Assert.Equal(260m, _host.LoadOrder(orderId).TotalAmount);
    }

    [Fact]
    public async Task AddItem_CustomItemWithoutSpice_DefaultsToOne()
    {
        var orderId = _host.SeedOrder();

        await _host.CreateController().AddItem(orderId, AddCommand(CustomItemId));

        Assert.Equal(1, Assert.Single(_host.LoadOrder(orderId).Items).SpiceLevel);
    }

    [Fact]
    public async Task AddItem_AddOnsOnItemNotSupportingThem_AreIgnored()
    {
        var orderId = _host.SeedOrder();

        await _host.CreateController().AddItem(orderId, AddCommand(SimpleItemId, 1, null, (EggAddOnId, 1)));

        var order = _host.LoadOrder(orderId);
        Assert.Empty(Assert.Single(order.Items).AddOns);
        Assert.Equal(100m, order.TotalAmount);
    }

    [Fact]
    public async Task AddItem_InactiveAddOn_RejectedAndNothingAdded()
    {
        var orderId = _host.SeedOrder();
        var controller = _host.CreateController();

        await controller.AddItem(orderId, AddCommand(CustomItemId, 1, null, (InactiveAddOnId, 1)));

        Assert.Equal("部分加料已下架，請重新選擇。", controller.TempData["Error"]);
        Assert.Empty(_host.LoadOrder(orderId).Items);
    }

    [Fact]
    public async Task AddItem_InactiveMenuItem_Rejected()
    {
        var orderId = _host.SeedOrder();
        var controller = _host.CreateController();

        await controller.AddItem(orderId, AddCommand(InactiveItemId));

        Assert.Equal("品項不存在或已下架。", controller.TempData["Error"]);
        Assert.Empty(_host.LoadOrder(orderId).Items);
    }

    [Fact]
    public async Task AddItem_PriceChangedLater_ExistingItemKeepsOrderedPrice()
    {
        var orderId = _host.SeedOrder();
        await _host.CreateController().AddItem(orderId, AddCommand(SimpleItemId));

        using (var db = _host.CreateDbContext())
        {
            db.MenuItems.Find(SimpleItemId)!.Price = 999m;
            db.SaveChanges();
        }

        // 再加點一份同品項：新品項用新價，舊品項維持點餐當下的價格
        await _host.CreateController().AddItem(orderId, AddCommand(SimpleItemId));
        var order = _host.LoadOrder(orderId);
        Assert.Equal([100m, 999m], order.Items.OrderBy(i => i.Id).Select(i => i.UnitPrice));
        Assert.Equal(1099m, order.TotalAmount);
    }

    [Theory]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Voided)]
    public async Task AddItem_ClosedOrder_Rejected(OrderStatus status)
    {
        var orderId = _host.SeedOrder(status, Item(isPrinted: true));
        var controller = _host.CreateController();

        await controller.AddItem(orderId, AddCommand(SimpleItemId));

        Assert.Equal("訂單已鎖定，無法加點。", controller.TempData["Error"]);
        Assert.Single(_host.LoadOrder(orderId).Items);
    }

    [Fact]
    public async Task RemoveItem_RecalculatesTotal()
    {
        var orderId = _host.SeedOrder(OrderStatus.Open, Item(quantity: 2), Item(quantity: 1));
        var removeId = _host.LoadOrder(orderId).Items.OrderBy(i => i.Id).First().Id;

        await _host.CreateController().RemoveItem(orderId, removeId);

        var order = _host.LoadOrder(orderId);
        Assert.Single(order.Items);
        Assert.Equal(100m, order.TotalAmount);
    }

    [Fact]
    public async Task CancelPendingItems_NothingEverPrinted_VoidsOrder()
    {
        var orderId = _host.SeedOrder(OrderStatus.Open, Item(), Item());

        await _host.CreateController().CancelPendingItems(orderId);

        var order = _host.LoadOrder(orderId);
        Assert.Empty(order.Items);
        Assert.Equal(OrderStatus.Voided, order.Status);
        Assert.Equal(EmployeeId, order.VoidedByEmployeeId);
    }

    // ── 出單（PassPRNT） ────────────────────────────────────────

    [Fact]
    public async Task ConfirmPrint_FirstBatch_PrintsFullOrderWithoutMarkingItems()
    {
        var orderId = _host.SeedOrder(OrderStatus.Open, Item(), Item());

        var result = await _host.CreateController().ConfirmPrint(orderId);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Print", view.ViewName);
        var model = Assert.IsType<PrintTicketViewModel>(view.Model);
        Assert.Equal("全　單", model.Title);
        Assert.Equal(2, model.Items.Count);
        var ids = _host.LoadOrder(orderId).Items.Select(i => i.Id);
        Assert.Contains($"items={string.Join(',', ids)}", model.ResultPath);
        // 要等 PassPRNT 回報成功才標記已出單
        Assert.All(_host.LoadOrder(orderId).Items, i => Assert.False(i.IsPrinted));
    }

    [Fact]
    public async Task ConfirmPrint_AfterPrinted_PrintsOnlyAddedItems()
    {
        var orderId = _host.SeedOrder(OrderStatus.Open, Item(isPrinted: true), Item(quantity: 2));

        var view = Assert.IsType<ViewResult>(await _host.CreateController().ConfirmPrint(orderId));

        var model = Assert.IsType<PrintTicketViewModel>(view.Model);
        Assert.Equal("加　點", model.Title);
        Assert.Equal("訂單目前總金額", model.TotalLabel);
        Assert.Equal(2, Assert.Single(model.Items).Quantity);
    }

    [Fact]
    public async Task ConfirmPrint_NoPendingItems_ShowsError()
    {
        var orderId = _host.SeedOrder(OrderStatus.Open, Item(isPrinted: true));
        var controller = _host.CreateController();

        Assert.IsType<RedirectToActionResult>(await controller.ConfirmPrint(orderId));
        Assert.Equal("目前沒有尚未出單的品項。", controller.TempData["Error"]);
    }

    [Fact]
    public async Task ConfirmPrint_CompletedOrder_Rejected()
    {
        var orderId = _host.SeedOrder(OrderStatus.Completed, Item());
        var controller = _host.CreateController();

        Assert.IsType<RedirectToActionResult>(await controller.ConfirmPrint(orderId));
        Assert.Equal("訂單已鎖定，無法出單。", controller.TempData["Error"]);
    }

    [Fact]
    public void PrintResult_Failed_ShowsWarningAndRedirects()
    {
        var controller = _host.CreateController();

        var result = controller.PrintResult(5, "1,2", "1", "Paper Empty");

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Contains("出單失敗（Paper Empty）", (string)controller.TempData["Warning"]!);
    }

    [Fact]
    public void PrintResult_Succeeded_AsksToMarkPrintedViaPost()
    {
        var view = Assert.IsType<ViewResult>(_host.CreateController().PrintResult(5, "1,2", "0", null));

        Assert.Equal("PrintSucceeded", view.ViewName);
        Assert.Equal(new PrintSucceededViewModel(5, "1,2"), view.Model);
    }

    [Fact]
    public async Task MarkPrinted_MarksOnlyItemsSentToPrinter()
    {
        var orderId = _host.SeedOrder(OrderStatus.Open, Item(), Item());
        var ids = _host.LoadOrder(orderId).Items.OrderBy(i => i.Id).Select(i => i.Id).ToList();

        await _host.CreateController().MarkPrinted(orderId, ids[0].ToString());

        var order = _host.LoadOrder(orderId);
        Assert.True(order.Items.Single(i => i.Id == ids[0]).IsPrinted);
        Assert.False(order.Items.Single(i => i.Id == ids[1]).IsPrinted);
    }

    [Fact]
    public async Task Reprint_VoidedOrder_Rejected()
    {
        var orderId = _host.SeedOrder(OrderStatus.Voided, Item(isPrinted: true));
        var controller = _host.CreateController();

        Assert.IsType<RedirectToActionResult>(await controller.Reprint(orderId));
        Assert.Equal("訂單已作廢，無法補印。", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Reprint_DoesNotCarryItemIds()
    {
        var orderId = _host.SeedOrder(OrderStatus.Completed, Item(isPrinted: true));

        var view = Assert.IsType<ViewResult>(await _host.CreateController().Reprint(orderId));

        var model = Assert.IsType<PrintTicketViewModel>(view.Model);
        Assert.Equal("補　印", model.Title);
        Assert.DoesNotContain("items=", model.ResultPath);
    }

    // ── 結帳與作廢（狀態轉換） ─────────────────────────────────

    [Fact]
    public async Task Checkout_AllPrintedAndPaid_CompletesOrder()
    {
        var orderId = _host.SeedOrder(OrderStatus.Open, Item(quantity: 2, isPrinted: true));
        var controller = _host.CreateController();

        await controller.Checkout(orderId, 500m);

        var order = _host.LoadOrder(orderId);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.NotNull(order.CompletedAt);
        Assert.Equal("結帳完成，收 $500，找零 $300。", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Checkout_PendingItems_Blocked()
    {
        var orderId = _host.SeedOrder(OrderStatus.Open, Item(isPrinted: true), Item());
        var controller = _host.CreateController();

        await controller.Checkout(orderId, 500m);

        Assert.Equal(OrderStatus.Open, _host.LoadOrder(orderId).Status);
        Assert.Equal("尚有 1 項未出單，請先確認訂單或取消這些品項。", controller.TempData["Error"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(99)]
    public async Task Checkout_MissingOrInsufficientPayment_Blocked(int? received)
    {
        var orderId = _host.SeedOrder(OrderStatus.Open, Item(isPrinted: true));
        var controller = _host.CreateController();

        await controller.Checkout(orderId, received);

        Assert.Equal(OrderStatus.Open, _host.LoadOrder(orderId).Status);
        Assert.NotNull(controller.TempData["Error"]);
    }

    [Fact]
    public async Task Checkout_EmptyOrder_Blocked()
    {
        var orderId = _host.SeedOrder();

        await _host.CreateController().Checkout(orderId, 0m);

        Assert.Equal(OrderStatus.Open, _host.LoadOrder(orderId).Status);
    }

    [Fact]
    public async Task Checkout_AlreadyCompleted_Blocked()
    {
        var orderId = _host.SeedOrder(OrderStatus.Completed, Item(isPrinted: true));
        var controller = _host.CreateController();

        await controller.Checkout(orderId, 100m);

        Assert.Equal("訂單狀態不允許結帳，或尚未加入任何品項。", controller.TempData["Error"]);
    }

    [Theory]
    [InlineData(OrderStatus.Open)]
    [InlineData(OrderStatus.Completed)]
    public async Task Void_OpenOrCompleted_RecordsReasonAndOperator(OrderStatus status)
    {
        var orderId = _host.SeedOrder(status, Item(isPrinted: true));

        await _host.CreateController().Void(orderId, new VoidOrderCommand("客人取消"));

        var order = _host.LoadOrder(orderId);
        Assert.Equal(OrderStatus.Voided, order.Status);
        Assert.Equal("客人取消", order.VoidReason);
        Assert.Equal(EmployeeId, order.VoidedByEmployeeId);
    }

    [Fact]
    public async Task Void_AlreadyVoided_Rejected()
    {
        var orderId = _host.SeedOrder(OrderStatus.Voided, Item(isPrinted: true));
        var controller = _host.CreateController();

        await controller.Void(orderId, new VoidOrderCommand("再作廢一次"));

        Assert.Equal("此訂單狀態不可作廢。", controller.TempData["Error"]);
    }

    [Fact]
    public async Task UnknownOrder_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await _host.CreateController().Checkout(999, 100m));
        Assert.IsType<NotFoundResult>(await _host.CreateController().ConfirmPrint(999));
    }
}
