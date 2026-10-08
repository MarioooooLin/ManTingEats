using System.Security.Claims;
using ManTingEats.Data;
using ManTingEats.Models;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using ManTingEats.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManTingEats.Controllers;

[Authorize]
public sealed class OrderController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrderController> _logger;

    public OrderController(AppDbContext db, ILogger<OrderController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private int CurrentEmployeeId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index(int page = 1)
    {
        // 未結帳訂單一律排最前面：分頁後若只依建立時間排序，忙碌時較早開的未結帳單會被擠到第二頁而漏結帳
        var query = _db.Orders
            .OrderBy(o => o.Status == OrderStatus.Open ? 0 : 1)
            .ThenByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id);
        return View(await PagedList<Order>.CreateAsync(query, page));
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateOrderCommand(OrderChannel.DineIn, null));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderCommand command, bool confirmDuplicateTable = false)
    {
        // 去除前後空白，避免「A1」與「A1 」被視為不同桌而漏掉同桌未結帳提醒
        command = command with { TableNumber = command.TableNumber?.Trim() };

        if (command.Channel == OrderChannel.DineIn && string.IsNullOrWhiteSpace(command.TableNumber))
        {
            ModelState.AddModelError(nameof(command.TableNumber), "內用訂單請填寫桌號");
        }

        if (!ModelState.IsValid)
        {
            return View(command);
        }

        // 同桌可能因併桌而合理地開第二張單，僅提示不強制擋下，需使用者主動勾選確認
        if (command.Channel == OrderChannel.DineIn && !confirmDuplicateTable)
        {
            var existingOpenOrder = await _db.Orders
                .Where(o => o.Status == OrderStatus.Open && o.TableNumber == command.TableNumber)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
            if (existingOpenOrder is not null)
            {
                ViewBag.DuplicateTableOrder = existingOpenOrder;
                return View(command);
            }
        }

        var order = new Order
        {
            Channel = command.Channel,
            TableNumber = command.Channel == OrderChannel.DineIn ? command.TableNumber : null,
            Status = OrderStatus.Open,
            TotalAmount = 0,
            DailyNumber = await GetNextDailyNumberAsync(),
            CreatedByEmployeeId = CurrentEmployeeId
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    /// <summary>以台灣時區的營業日（06:00 切換，v9）判斷邊界，計算本營業日下一個流水號；跨午夜營業時單號不會歸零。</summary>
    private async Task<int> GetNextDailyNumberAsync()
    {
        var dayStartUtc = TaipeiTime.BusinessDayStartUtc(TaipeiTime.BusinessToday);
        var dayEndUtc = dayStartUtc.AddDays(1);

        var maxDailyNumber = await _db.Orders
            .Where(o => o.CreatedAt >= dayStartUtc && o.CreatedAt < dayEndUtc)
            .Select(o => (int?)o.DailyNumber)
            .MaxAsync();

        return (maxDailyNumber ?? 0) + 1;
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .Include(o => o.CreatedByEmployee)
            .Include(o => o.VoidedByEmployee)
            .Include(o => o.DiscountedByEmployee)
            .SingleOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        var activeMenuItems = await _db.MenuItems
            .Where(m => m.IsActive)
            .OrderBy(m => m.Category).ThenBy(m => m.Name)
            .ToListAsync();
        var activeAddOns = await _db.AddOns
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return View(new OrderDetailsViewModel { Order = order, ActiveMenuItems = activeMenuItems, ActiveAddOns = activeAddOns });
    }

    /// <summary>包含加料金額的訂單總額重算，項目集合需已 Include AddOns。</summary>
    private static decimal ComputeTotal(Order order) =>
        order.Items.Sum(i => i.Subtotal);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(int orderId, AddOrderItemCommand command)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .SingleOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join("；", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        if (order.Status != OrderStatus.Open)
        {
            TempData["Error"] = "訂單已鎖定，無法加點。";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        var menuItem = await _db.MenuItems.SingleOrDefaultAsync(m => m.Id == command.MenuItemId && m.IsActive);
        if (menuItem is null)
        {
            TempData["Error"] = "品項不存在或已下架。";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // 客製化品項也可點多份（v10）：辣度、加料、備註套用到每一份，加料數量為每份的量
        var newItem = new OrderItem
        {
            MenuItemId = menuItem.Id,
            UnitPrice = menuItem.Price,
            Quantity = command.Quantity,
            Note = command.Note,
            SpiceLevel = menuItem.SupportsSpiceLevel ? (command.SpiceLevel ?? OrderLimits.DefaultSpiceLevel) : null
        };

        if (menuItem.SupportsAddOns && command.AddOns is { Count: > 0 })
        {
            var addOnIds = command.AddOns.Select(a => a.AddOnId).Distinct().ToList();
            var activeAddOns = await _db.AddOns.Where(a => addOnIds.Contains(a.Id) && a.IsActive).ToListAsync();
            if (activeAddOns.Count != addOnIds.Count)
            {
                TempData["Error"] = "部分加料已下架，請重新選擇。";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }

            foreach (var selection in command.AddOns)
            {
                var addOn = activeAddOns.Single(a => a.Id == selection.AddOnId);
                newItem.AddOns.Add(new OrderItemAddOn
                {
                    AddOnId = addOn.Id,
                    AddOnName = addOn.Name,
                    UnitPrice = addOn.Price,
                    Quantity = selection.Quantity
                });
            }
        }

        order.Items.Add(newItem);
        // 每次加點皮重新加總，確保金額與品項清單一致（含加料）
        order.TotalAmount = ComputeTotal(order);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"已加點 {menuItem.Name} x{newItem.Quantity}。";

        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPrint(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .SingleOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return NotFound();
        }

        // 確認出單僅限未結帳訂單；結帳前必須先出完單，作廢訂單則不應再送廚房
        if (order.Status != OrderStatus.Open)
        {
            TempData["Error"] = "訂單已鎖定，無法出單。";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        var pendingItems = OrderTicket.PendingItems(order);
        if (pendingItems.Count == 0)
        {
            TempData["Error"] = "目前沒有尚未出單的品項。";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // 此處只產生出單內容、不標記已出單：要等 PassPRNT 回報成功（PrintResult → MarkPrinted）才算數
        var isFirstBatch = OrderTicket.IsFirstBatch(order);
        return View("Print", new PrintTicketViewModel
        {
            Order = order,
            Title = isFirstBatch ? "全　單" : "加　點",
            Items = isFirstBatch ? order.Items.ToList() : pendingItems,
            TotalLabel = isFirstBatch ? "總金額" : "訂單目前總金額",
            PrintedAt = TaipeiTime.Now,
            ResultPath = Url.Action(nameof(PrintResult), new { orderId, items = OrderTicket.FormatItemIds(pendingItems) })!
        });
    }

    /// <summary>
    /// PassPRNT 印完後回到此處（GET），帶回 passprnt_code（0 = 成功）與 passprnt_message。
    /// items 為送印當下的待出單品項 ID；補印時為空，成功與否都不改變出單狀態。
    /// </summary>
    [HttpGet]
    public IActionResult PrintResult(
        int orderId,
        string? items,
        [FromQuery(Name = "passprnt_code")] string? printCode,
        [FromQuery(Name = "passprnt_message")] string? printMessage)
    {
        var isReprint = string.IsNullOrEmpty(items);
        var succeeded = printCode == "0";
        _logger.LogInformation("訂單 {OrderId} {TicketKind}列印結果：代碼 {Code}，訊息 {Message}",
            orderId, isReprint ? "補印" : "出單", printCode, printMessage);

        if (succeeded && !isReprint)
        {
            return View("PrintSucceeded", new PrintSucceededViewModel(orderId, items!));
        }

        if (succeeded)
        {
            TempData["Success"] = "已補印。";
        }
        else
        {
            // 定案決議：只要不是成功一律視為未出單，由店長處理印表機後人工重印（v5 第 6 節）
            var reason = string.IsNullOrWhiteSpace(printMessage) ? $"代碼 {printCode}" : printMessage;
            TempData["Warning"] = isReprint
                ? $"⬆ 補印失敗（{reason}），請確認印表機後再按「補印出單」。"
                : $"⬆ 出單失敗（{reason}），請確認印表機（紙張、電源、USB）後重新按「確認訂單」。";
        }

        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPrinted(int orderId, string? items)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return NotFound();
        }

        // 不檢查訂單狀態：紙本已實際印出，出單狀態應如實反映
        OrderTicket.MarkPrinted(order, OrderTicket.ParseItemIds(items));
        await _db.SaveChangesAsync();
        TempData["Success"] = "已出單。";
        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelPendingItems(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .SingleOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Open)
        {
            TempData["Error"] = "訂單已鎖定，無法取消。";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        var pendingItems = order.Items.Where(i => !i.IsPrinted).ToList();
        foreach (var item in pendingItems)
        {
            order.Items.Remove(item);
            _db.OrderItems.Remove(item);
        }
        order.TotalAmount = ComputeTotal(order);

        // 訂單從未出過單，代表廚房從未收到通知，直接連同訂單一併作廢，不需使用者輸入理由
        if (order.Items.Count == 0)
        {
            order.Status = OrderStatus.Voided;
            order.VoidReason = "未確認出單，使用者取消";
            order.VoidedByEmployeeId = CurrentEmployeeId;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "已取消。";
        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(int orderId, int orderItemId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .SingleOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Open)
        {
            TempData["Error"] = "訂單已鎖定，無法移除品項。";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        var item = order.Items.SingleOrDefault(i => i.Id == orderItemId);
        if (item is not null)
        {
            order.Items.Remove(item);
            _db.OrderItems.Remove(item);
            order.TotalAmount = ComputeTotal(order);
            await _db.SaveChangesAsync();
            TempData["Success"] = "已移除品項。";
        }

        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(
        int id,
        decimal? receivedAmount,
        decimal? discountedAmount = null,
        DiscountReason? discountReason = null,
        string? discountNote = null)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .SingleOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Open || order.Items.Count == 0)
        {
            TempData["Error"] = "訂單狀態不允許結帳，或尚未加入任何品項。";
            return RedirectToAction(nameof(Details), new { id });
        }

        // 結帳不補印（v5 決議）：待出單品項代表廚房從未收到，必須先出單或取消，避免漏做卻照收錢
        var pendingCount = OrderTicket.PendingItems(order).Count;
        if (pendingCount > 0)
        {
            TempData["Error"] = $"尚有 {pendingCount} 項未出單，請先確認訂單或取消這些品項。";
            return RedirectToAction(nameof(Details), new { id });
        }

        // 折扣（v8）：前端把改收金額／打折／抹零都換算成折扣後應收金額送來；未給折扣時為 null
        var note = string.IsNullOrWhiteSpace(discountNote) ? null : discountNote.Trim();
        if (discountedAmount is not null)
        {
            var discountError = OrderDiscount.Validate(order.TotalAmount, discountedAmount.Value, discountReason, note);
            if (discountError is not null)
            {
                TempData["Error"] = discountError;
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        var amountDue = discountedAmount ?? order.TotalAmount;

        // 收款金額只用來檢查與算找零，不存資料庫（v6 決議）；後端仍需檢查，防止繞過結帳視窗直接送出
        var paymentError = CashPayment.Validate(receivedAmount, amountDue);
        if (paymentError is not null)
        {
            TempData["Error"] = paymentError;
            return RedirectToAction(nameof(Details), new { id });
        }

        if (discountedAmount is not null)
        {
            // 折扣影響實收與營收報表，需留下金額、原因與操作者供事後對帳
            order.DiscountAmount = order.TotalAmount - discountedAmount.Value;
            order.DiscountReason = discountReason;
            order.DiscountNote = note;
            order.DiscountedByEmployeeId = CurrentEmployeeId;
        }

        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        // 找零放在成功訊息中，店長找錢時可再看一次
        var discountText = order.DiscountAmount > 0 ? $"（折扣 ${order.DiscountAmount:F0}，應收 ${amountDue:F0}）" : string.Empty;
        TempData["Success"] = $"結帳完成{discountText}，收 ${receivedAmount!.Value:F0}，找零 ${receivedAmount.Value - amountDue:F0}。";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(int id, VoidOrderCommand command)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Open && order.Status != OrderStatus.Completed)
        {
            TempData["Error"] = "此訂單狀態不可作廢。";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join("；", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id });
        }

        // 作廢需留下操作者與原因，供稽核追蹤；不列印作廢通知，由店長在原出單紙本上手動打叉
        order.Status = OrderStatus.Voided;
        order.VoidReason = command.Reason;
        order.VoidedByEmployeeId = CurrentEmployeeId;
        await _db.SaveChangesAsync();
        TempData["Success"] = "訂單已作廢。";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reprint(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .SingleOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        // 已作廢訂單若再印出「全單」，廚房可能誤以為要製作，一律不允許補印
        if (order.Status == OrderStatus.Voided)
        {
            TempData["Error"] = "訂單已作廢，無法補印。";
            return RedirectToAction(nameof(Details), new { id });
        }

        // 補印只是重印一張紙，不改變任何品項的出單狀態，因此回呼網址不帶品項 ID
        return View("Print", new PrintTicketViewModel
        {
            Order = order,
            Title = "補　印",
            Items = order.Items.ToList(),
            TotalLabel = "總金額",
            PrintedAt = TaipeiTime.Now,
            ResultPath = Url.Action(nameof(PrintResult), new { orderId = order.Id })!
        });
    }
}
