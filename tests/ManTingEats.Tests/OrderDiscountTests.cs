using ManTingEats.Controllers;
using ManTingEats.Models;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using ManTingEats.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static ManTingEats.Tests.OrderControllerTestHost;

namespace ManTingEats.Tests;

public sealed class OrderDiscountTests : IDisposable
{
    private readonly OrderControllerTestHost _host = new();

    public void Dispose() => _host.Dispose();

    // ── 折扣規則 ──────────────────────────────────────────────

    [Theory]
    [InlineData(487, 480)]
    [InlineData(1235, 1230)]
    [InlineData(9, 0)]
    public void RoundOff_DropsUnitsDigit(int total, int expected)
    {
        Assert.Equal(expected, OrderDiscount.RoundOff(total));
    }

    [Theory]
    [InlineData(500, 9, 450)]     // 9 折
    [InlineData(500, 85, 425)]    // 85 折
    [InlineData(487, 9, 438)]     // 438.3 → 438
    [InlineData(485, 9, 437)]     // 436.5 → 437（四捨五入）
    public void ApplyRate_ConvertsRateAndRounds(int total, int rate, int expected)
    {
        Assert.Equal(expected, OrderDiscount.ApplyRate(total, rate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-5)]
    public void ApplyRate_OutOfRange_ReturnsNull(int rate)
    {
        Assert.Null(OrderDiscount.ApplyRate(500, rate));
    }

    [Theory]
    [InlineData(450, DiscountReason.RegularCustomer, null)]
    [InlineData(0, DiscountReason.Treat, null)]                     // 整單招待
    [InlineData(450, DiscountReason.FoodIssue, "牛肉麵太鹹")]
    public void Validate_ValidDiscount_ReturnsNull(int due, DiscountReason reason, string? note)
    {
        Assert.Null(OrderDiscount.Validate(500, due, reason, note));
    }

    [Theory]
    [InlineData(500, DiscountReason.RegularCustomer, null, "折扣後金額需小於原價。")]
    [InlineData(-1, DiscountReason.RegularCustomer, null, "折扣後金額不可小於 0。")]
    [InlineData(450, null, null, "請選擇折扣原因。")]
    [InlineData(450, DiscountReason.FoodIssue, "  ", "折扣原因為「餐點問題」時，請填寫說明。")]
    [InlineData(450, DiscountReason.Other, null, "折扣原因為「其他」時，請填寫說明。")]
    public void Validate_InvalidDiscount_ReturnsError(int due, DiscountReason? reason, string? note, string expected)
    {
        Assert.Equal(expected, OrderDiscount.Validate(500, due, reason, note));
    }

    [Fact]
    public void Validate_FractionalDueOrLongNote_ReturnsError()
    {
        Assert.NotNull(OrderDiscount.Validate(500, 449.5m, DiscountReason.RegularCustomer, null));
        Assert.NotNull(OrderDiscount.Validate(500, 450, DiscountReason.Other, new string('字', 101)));
    }

    // ── 結帳套用折扣 ────────────────────────────────────────────

    [Fact]
    public async Task Checkout_WithDiscount_SavesDiscountAndValidatesPaymentAgainstDiscountedAmount()
    {
        var orderId = _host.SeedOrder(OrderStatus.Open, Item(quantity: 5, isPrinted: true));   // 原價 $500
        var controller = _host.CreateController();

        await controller.Checkout(orderId, receivedAmount: 450m, discountedAmount: 450m, DiscountReason.RegularCustomer, "  老客人  ");

        var order = _host.LoadOrder(orderId);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(500m, order.TotalAmount);          // 原價不變
        Assert.Equal(50m, order.DiscountAmount);
        Assert.Equal(450m, order.AmountDue);
        Assert.Equal(DiscountReason.RegularCustomer, order.DiscountReason);
        Assert.Equal("老客人", order.DiscountNote);
        Assert.Equal(EmployeeId, order.DiscountedByEmployeeId);
        Assert.Equal("結帳完成（折扣 $50，應收 $450），收 $450，找零 $0。", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Checkout_FullTreat_AllowsZeroPayment()
    {
        var orderId = _host.SeedOrder(OrderStatus.Open, Item(isPrinted: true));

        await _host.CreateController().Checkout(orderId, receivedAmount: 0m, discountedAmount: 0m, DiscountReason.Treat);

        var order = _host.LoadOrder(orderId);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(0m, order.AmountDue);
    }

    [Fact]
    public async Task Checkout_DiscountWithoutRequiredNote_Blocked()
    {
        var orderId = _host.SeedOrder(OrderStatus.Open, Item(quantity: 5, isPrinted: true));
        var controller = _host.CreateController();

        await controller.Checkout(orderId, receivedAmount: 450m, discountedAmount: 450m, DiscountReason.FoodIssue, null);

        var order = _host.LoadOrder(orderId);
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.Equal(0m, order.DiscountAmount);
        Assert.Equal("折扣原因為「餐點問題」時，請填寫說明。", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Checkout_PaymentBelowDiscountedAmount_Blocked()
    {
        var orderId = _host.SeedOrder(OrderStatus.Open, Item(quantity: 5, isPrinted: true));
        var controller = _host.CreateController();

        await controller.Checkout(orderId, receivedAmount: 400m, discountedAmount: 450m, DiscountReason.RegularCustomer);

        Assert.Equal(OrderStatus.Open, _host.LoadOrder(orderId).Status);
        Assert.Equal("收到金額不足，還差 $50。", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Checkout_WithoutDiscount_LeavesDiscountFieldsEmpty()
    {
        var orderId = _host.SeedOrder(OrderStatus.Open, Item(isPrinted: true));

        // 未開啟折扣時前端不送原因；即使送了也不應寫入
        await _host.CreateController().Checkout(orderId, receivedAmount: 100m, discountedAmount: null, DiscountReason.Treat);

        var order = _host.LoadOrder(orderId);
        Assert.Equal(0m, order.DiscountAmount);
        Assert.Null(order.DiscountReason);
        Assert.Null(order.DiscountedByEmployeeId);
    }

    // ── 營收報表 ──────────────────────────────────────────────

    [Fact]
    public async Task Report_UsesAmountAfterDiscount_AndShowsTotalDiscount()
    {
        var discounted = _host.SeedOrder(OrderStatus.Open, Item(quantity: 5, isPrinted: true));   // $500 → 折到 $450
        var regular = _host.SeedOrder(OrderStatus.Open, Item(quantity: 2, isPrinted: true));      // $200
        await _host.CreateController().Checkout(discounted, 450m, 450m, DiscountReason.RegularCustomer);
        await _host.CreateController().Checkout(regular, 200m);

        var result = await new ReportController(_host.CreateDbContext()).Index(null, null);
        var report = Assert.IsType<ReportViewModel>(Assert.IsType<ViewResult>(result).Model);

        Assert.Equal(650m, report.TotalRevenue);
        Assert.Equal(50m, report.TotalDiscount);
        Assert.Equal(650m, Assert.Single(report.ChannelBreakdown).Amount);
        // 品項排行維持原價：品項 + 加料 − 折扣 = 總營收
        Assert.Equal(700m, report.ItemRanking.Sum(i => i.Amount) + report.AddOnRanking.Sum(a => a.Amount));
        Assert.Equal(report.TotalRevenue,
            report.ItemRanking.Sum(i => i.Amount) + report.AddOnRanking.Sum(a => a.Amount) - report.TotalDiscount);
    }
}
