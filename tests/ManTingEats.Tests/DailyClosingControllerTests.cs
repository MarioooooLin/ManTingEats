using System.Reflection;
using System.Security.Claims;
using ManTingEats.Controllers;
using ManTingEats.Models;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using ManTingEats.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Xunit;
using static ManTingEats.Tests.OrderControllerTestHost;

namespace ManTingEats.Tests;

public sealed class DailyClosingControllerTests : IDisposable
{
    // 沿用 OrderController 的測試環境，取其中已建立的員工與菜單
    private readonly OrderControllerTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private static DateTime Today => TaipeiTime.BusinessToday;
    private static DateTime TodayStartUtc => TaipeiTime.BusinessDayStartUtc(Today);

    private DailyClosingController CreateController(decimal? configuredOpeningCash = null)
    {
        var settings = new Dictionary<string, string?>();
        if (configuredOpeningCash.HasValue)
        {
            settings["DailyClosing:DefaultOpeningCash"] = configuredOpeningCash.Value.ToString();
        }

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, EmployeeId.ToString())], "Test"))
        };
        return new DailyClosingController(
            _host.CreateDbContext(),
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new NullTempDataProvider())
        };
    }

    /// <summary>建立一張已結帳訂單：原價 = 份數 x $100，結帳時間由測試指定。</summary>
    private void SeedCompleted(DateTime completedAtUtc, int quantity, decimal discount = 0m)
    {
        var id = _host.SeedOrder(OrderStatus.Completed, Item(quantity));
        using var db = _host.CreateDbContext();
        var order = db.Orders.Single(o => o.Id == id);
        order.CreatedAt = completedAtUtc.AddMinutes(-30);
        order.CompletedAt = completedAtUtc;
        order.DiscountAmount = discount;
        db.SaveChanges();
    }

    private void SeedOrderCreatedAt(OrderStatus status, DateTime createdAtUtc)
    {
        var id = _host.SeedOrder(status);
        using var db = _host.CreateDbContext();
        db.Orders.Single(o => o.Id == id).CreatedAt = createdAtUtc;
        db.SaveChanges();
    }

    private void SeedExpense(DateTime date, decimal amount)
    {
        using var db = _host.CreateDbContext();
        db.Expenses.Add(new Expense { Date = date, Category = ExpenseCategory.Ingredients, Amount = amount });
        db.SaveChanges();
    }

    private async Task<DailyClosingViewModel> IndexAsync(DateTime? date = null, decimal? configuredOpeningCash = null) =>
        Assert.IsType<DailyClosingViewModel>(Assert.IsType<ViewResult>(
            await CreateController(configuredOpeningCash).Index(date)).Model);

    private static CloseDayCommand Command(decimal counted, decimal opening = 15000m, string? note = null, DateTime? date = null) =>
        new(date ?? Today, opening, counted, note);

    private List<DailyClosing> LoadClosings()
    {
        using var db = _host.CreateDbContext();
        return db.DailyClosings.OrderBy(c => c.Id).ToList();
    }

    // ── 彙總與公式 ──────────────────────────────────────────────

    [Fact]
    public async Task Index_SummaryUsesBusinessDayRevenueAndExpense()
    {
        SeedCompleted(TodayStartUtc.AddHours(12), quantity: 3);                  // 18:00 結帳 $300
        SeedCompleted(TodayStartUtc.AddHours(20), quantity: 2, discount: 50m);   // 隔天 02:00 仍屬今天：$200 折 $50
        SeedCompleted(TodayStartUtc.AddHours(-1), quantity: 5);                  // 前一營業日：不算
        SeedOrderCreatedAt(OrderStatus.Voided, TodayStartUtc.AddHours(13));
        SeedExpense(Today, 120m);
        SeedExpense(Today.AddDays(-1), 999m);                                     // 前一營業日：不算

        var model = await IndexAsync();

        Assert.Equal(new DailyClosingSummary(450m, 120m, 50m, 2, 1), model.Summary);
        Assert.Equal(15000m, model.Form.OpeningCash);
        Assert.False(model.IsReclose);
    }

    [Fact]
    public async Task Index_DefaultOpeningCashComesFromSettings()
    {
        var model = await IndexAsync(configuredOpeningCash: 20000m);

        Assert.Equal(20000m, model.Form.OpeningCash);
    }

    [Fact]
    public async Task Close_SavesSnapshotWithExpectedCashAndDifference()
    {
        SeedCompleted(TodayStartUtc.AddHours(12), quantity: 3);   // 營收 $300
        SeedExpense(Today, 120m);

        var result = await CreateController().Close(Command(counted: 15150m));

        Assert.IsType<RedirectToActionResult>(result);
        var closing = Assert.Single(LoadClosings());
        Assert.Equal(Today, closing.BusinessDate);
        Assert.Equal(300m, closing.Revenue);
        Assert.Equal(120m, closing.ExpenseTotal);
        Assert.Equal(15180m, closing.ExpectedCash);    // 15000 + 300 − 120
        Assert.Equal(-30m, closing.Difference);        // 少 $30
        Assert.Equal(EmployeeId, closing.ClosedByEmployeeId);
    }

    [Theory]
    [InlineData(10, "多 $10")]
    [InlineData(-25, "少 $25")]
    [InlineData(0, "現金相符")]
    public void DifferenceText_DescribesOverOrShort(decimal difference, string expected)
    {
        Assert.Equal(expected, DailyClosingController.DifferenceText(difference));
    }

    // ── 擋下的情況 ──────────────────────────────────────────────

    [Fact]
    public async Task Close_WithOpenOrders_IsRejected()
    {
        SeedOrderCreatedAt(OrderStatus.Open, TodayStartUtc.AddHours(-30));   // 前幾天遺留的未結帳單也要先處理
        var controller = CreateController();

        var result = await controller.Close(Command(counted: 15000m));

        var model = Assert.IsType<DailyClosingViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Single(model.OpenOrders);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(LoadClosings());
    }

    [Fact]
    public async Task Close_FutureBusinessDate_IsRejected()
    {
        var result = await CreateController().Close(Command(counted: 15000m, date: Today.AddDays(1)));

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(LoadClosings());
    }

    [Fact]
    public async Task Close_Reclose_RequiresNoteAndKeepsHistory()
    {
        await CreateController().Close(Command(counted: 15000m));
        SeedExpense(Today, 200m);   // 日結後補登支出

        var withoutNote = CreateController();
        Assert.IsType<ViewResult>(await withoutNote.Close(Command(counted: 14800m)));
        Assert.False(withoutNote.ModelState.IsValid);

        Assert.IsType<RedirectToActionResult>(await CreateController().Close(Command(counted: 14800m, note: "補登食材支出")));

        var closings = LoadClosings();
        Assert.Equal(2, closings.Count);
        Assert.Equal(0m, closings[0].ExpenseTotal);     // 第一次的快照不受之後補登影響
        Assert.Equal(200m, closings[1].ExpenseTotal);
        Assert.Equal(0m, closings[1].Difference);
        Assert.Equal("補登食材支出", closings[1].Note);
    }

    // ── 日結紀錄 ────────────────────────────────────────────────

    [Fact]
    public async Task History_ShowsLatestClosingPerDayWithCount()
    {
        await CreateController().Close(Command(counted: 15000m, date: Today.AddDays(-1)));
        await CreateController().Close(Command(counted: 15000m));
        await CreateController().Close(Command(counted: 15010m, note: "重新點收"));

        var result = await CreateController().History();

        var model = Assert.IsType<DailyClosingHistoryViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(2, model.Rows.Count);
        Assert.Equal(Today, model.Rows[0].Latest.BusinessDate);
        Assert.Equal(15010m, model.Rows[0].Latest.CountedCash);
        Assert.Equal(2, model.Rows[0].ClosingCount);
        Assert.Equal(1, model.Rows[1].ClosingCount);
    }

    [Theory]
    [InlineData(nameof(DailyClosingController.History))]
    [InlineData(nameof(DailyClosingController.Details))]
    public void HistoryActions_RequireManagerRole(string action)
    {
        var attribute = typeof(DailyClosingController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(AppRoles.Manager, attribute.Roles);
    }

    [Fact]
    public void ClosingActions_AreOpenToStaff()
    {
        var attribute = typeof(DailyClosingController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Null(attribute.Roles);
        Assert.Null(typeof(DailyClosingController).GetMethod(nameof(DailyClosingController.Close))!.GetCustomAttribute<AuthorizeAttribute>());
    }
}
