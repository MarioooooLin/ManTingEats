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

/// <summary>
/// 日結（v13）：打烊時點收現金並與系統金額對帳。店長與員工都能日結（打烊時店長不一定在場），
/// 日結紀錄只有店長能看。
/// </summary>
[Authorize]
public sealed class DailyClosingController : Controller
{
    /// <summary>未設定 DailyClosing:DefaultOpeningCash 時的零用金（收銀機與抽屜共 15,000）。</summary>
    public const decimal FallbackOpeningCash = 15000m;

    private readonly AppDbContext _db;
    private readonly decimal _defaultOpeningCash;

    public DailyClosingController(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _defaultOpeningCash = configuration.GetValue<decimal?>("DailyClosing:DefaultOpeningCash") ?? FallbackOpeningCash;
    }

    private int CurrentEmployeeId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index(DateTime? date)
    {
        var businessDate = (date ?? TaipeiTime.BusinessToday).Date;
        if (businessDate > TaipeiTime.BusinessToday)
        {
            TempData["Error"] = "不能日結未來的營業日。";
            return RedirectToAction(nameof(Index));
        }

        var model = await BuildViewModelAsync(businessDate, form: null);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close([Bind(Prefix = "Form")] CloseDayCommand command)
    {
        var businessDate = command.BusinessDate.Date;
        if (businessDate > TaipeiTime.BusinessToday)
        {
            TempData["Error"] = "不能日結未來的營業日。";
            return RedirectToAction(nameof(Index));
        }

        var model = await BuildViewModelAsync(businessDate, command);

        // 未結帳的單沒有收錢，硬結會讓當天數字不完整；畫面已列出這些單，這裡再擋一次避免同時有人開單
        if (model.OpenOrders.Count > 0)
        {
            ModelState.AddModelError(string.Empty, $"還有 {model.OpenOrders.Count} 張訂單未結帳，請先結帳或作廢後再日結。");
        }

        if (model.IsReclose && string.IsNullOrWhiteSpace(command.Note))
        {
            ModelState.AddModelError($"Form.{nameof(command.Note)}", "重新日結請填寫原因");
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), model);
        }

        // 營收、支出等以伺服器當下重新計算的值存成快照，不信任表單送來的數字
        var summary = model.Summary;
        var closing = new DailyClosing
        {
            BusinessDate = businessDate,
            OpeningCash = command.OpeningCash,
            Revenue = summary.Revenue,
            ExpenseTotal = summary.ExpenseTotal,
            DiscountTotal = summary.DiscountTotal,
            CompletedOrderCount = summary.CompletedOrderCount,
            VoidedOrderCount = summary.VoidedOrderCount,
            CountedCash = command.CountedCash!.Value,
            Note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim(),
            ClosedByEmployeeId = CurrentEmployeeId
        };
        _db.DailyClosings.Add(closing);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"已完成 {businessDate:yyyy-MM-dd} 日結，{DifferenceText(closing.Difference)}。";
        return RedirectToAction(nameof(Index), new { date = businessDate.ToString("yyyy-MM-dd") });
    }

    [Authorize(Roles = AppRoles.Manager)]
    public async Task<IActionResult> History(int page = 1)
    {
        // 每個營業日只列最後一次日結（Id 最大者），重結的次數另外計算
        var latestPerDay = _db.DailyClosings
            .Where(c => c.Id == _db.DailyClosings.Where(x => x.BusinessDate == c.BusinessDate).Max(x => x.Id))
            .Include(c => c.ClosedByEmployee)
            .OrderByDescending(c => c.BusinessDate)
            .ThenByDescending(c => c.Id);
        var paged = await PagedList<DailyClosing>.CreateAsync(latestPerDay, page);

        var dates = paged.Items.Select(c => c.BusinessDate).ToList();
        var counts = await _db.DailyClosings
            .Where(c => dates.Contains(c.BusinessDate))
            .GroupBy(c => c.BusinessDate)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Date, g => g.Count);

        return View(new DailyClosingHistoryViewModel
        {
            Page = paged,
            Rows = paged.Items.Select(c => new DailyClosingHistoryRow(c, counts[c.BusinessDate])).ToList()
        });
    }

    [Authorize(Roles = AppRoles.Manager)]
    public async Task<IActionResult> Details(DateTime date)
    {
        var closings = await _db.DailyClosings
            .Where(c => c.BusinessDate == date.Date)
            .Include(c => c.ClosedByEmployee)
            .OrderByDescending(c => c.Id)
            .ToListAsync();
        if (closings.Count == 0)
        {
            return NotFound();
        }

        ViewData["BusinessDate"] = date.Date;
        return View(closings);
    }

    /// <summary>差額的文字：正數為多、負數為少，讓點收的人不用自己判斷正負號。</summary>
    public static string DifferenceText(decimal difference) => difference switch
    {
        > 0 => $"多 ${difference:F0}",
        < 0 => $"少 ${-difference:F0}",
        _ => "現金相符"
    };

    private async Task<DailyClosingViewModel> BuildViewModelAsync(DateTime businessDate, CloseDayCommand? form)
    {
        var dayStartUtc = TaipeiTime.BusinessDayStartUtc(businessDate);
        var dayEndUtc = TaipeiTime.BusinessDayStartUtc(businessDate.AddDays(1));

        var latest = await _db.DailyClosings
            .Where(c => c.BusinessDate == businessDate)
            .Include(c => c.ClosedByEmployee)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync();

        // 包含前幾天遺留的未結帳單：它們若在當天之後才結帳，營收會算進結帳那天，日結前一併處理才不會漏
        var openOrders = await _db.Orders
            .Where(o => o.Status == OrderStatus.Open && o.CreatedAt < dayEndUtc)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        return new DailyClosingViewModel
        {
            BusinessDate = businessDate,
            Summary = await BuildSummaryAsync(businessDate, dayStartUtc, dayEndUtc),
            OpenOrders = openOrders,
            LatestClosing = latest,
            // 零用金沿用上次日結填的金額（重結時），否則用設定的預設值
            Form = form ?? new CloseDayCommand(businessDate, latest?.OpeningCash ?? _defaultOpeningCash, null, null)
        };
    }

    /// <summary>與營收報表相同的算法：營收依結帳時間落在營業日內的已結帳訂單實收；支出依營業日。</summary>
    private async Task<DailyClosingSummary> BuildSummaryAsync(DateTime businessDate, DateTime dayStartUtc, DateTime dayEndUtc)
    {
        var completed = _db.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= dayStartUtc && o.CompletedAt < dayEndUtc);

        var nextDate = businessDate.AddDays(1);
        var expenseTotal = await _db.Expenses
            .Where(e => e.Date >= businessDate && e.Date < nextDate)
            .SumAsync(e => e.Amount);

        // 作廢沒有記錄作廢時間，以開單時間判斷屬於哪個營業日
        var voidedCount = await _db.Orders
            .CountAsync(o => o.Status == OrderStatus.Voided && o.CreatedAt >= dayStartUtc && o.CreatedAt < dayEndUtc);

        return new DailyClosingSummary(
            Revenue: await completed.SumAsync(o => o.TotalAmount - o.DiscountAmount),
            ExpenseTotal: expenseTotal,
            DiscountTotal: await completed.SumAsync(o => o.DiscountAmount),
            CompletedOrderCount: await completed.CountAsync(),
            VoidedOrderCount: voidedCount);
    }
}
