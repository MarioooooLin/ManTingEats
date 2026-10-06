using ManTingEats.Data;
using ManTingEats.Models;
using ManTingEats.Models.Enums;
using ManTingEats.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManTingEats.Controllers;

[Authorize]
public sealed class ReportController : Controller
{
    private readonly AppDbContext _db;

    public ReportController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(DateTime? start, DateTime? end)
    {
        var startDate = (start ?? TaipeiTime.Today).Date;
        var endDate = (end ?? TaipeiTime.Today).Date;
        if (endDate < startDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        var endExclusive = endDate.AddDays(1);

        // CompletedAt 存 UTC，需將台灣營業日邊界換算為 UTC 再比對，否則 00:00–08:00 結帳的訂單會被算到前一天
        var startUtc = TaipeiTime.StartOfDayUtc(startDate);
        var endExclusiveUtc = TaipeiTime.StartOfDayUtc(endExclusive);

        // 報表僅計入已結帳訂單，不含已作廢訂單
        var completedOrders = await _db.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= startUtc && o.CompletedAt < endExclusiveUtc)
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .ToListAsync();

        var totalRevenue = completedOrders.Sum(o => o.TotalAmount);

        var totalExpense = await _db.Expenses
            .Where(e => e.Date >= startDate && e.Date < endExclusive)
            .SumAsync(e => e.Amount);

        var channelBreakdown = completedOrders
            .GroupBy(o => o.Channel)
            .Select(g => new ChannelRevenue(g.Key, g.Sum(o => o.TotalAmount)))
            .OrderByDescending(c => c.Amount)
            .ToList();

        var itemRanking = completedOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.MenuItem!.Name)
            .Select(g => new MenuItemSales(g.Key, g.Sum(i => i.Quantity), g.Sum(i => i.UnitPrice * i.Quantity)))
            .OrderByDescending(i => i.Amount)
            .ToList();

        // 加料以下單當下的名稱快照分組，加料日後改名或停用仍對得上歷史金額；
        // 金額算法與 OrderController.ComputeTotal 一致，品項與加料兩張排行相加才會等於總營收
        var addOnRanking = completedOrders
            .SelectMany(o => o.Items)
            .SelectMany(i => i.AddOns)
            .GroupBy(a => a.AddOnName)
            .Select(g => new AddOnSales(g.Key, g.Sum(a => a.Quantity), g.Sum(a => a.UnitPrice * a.Quantity)))
            .OrderByDescending(a => a.Amount)
            .ToList();

        return View(new ReportViewModel
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalRevenue = totalRevenue,
            TotalExpense = totalExpense,
            ChannelBreakdown = channelBreakdown,
            ItemRanking = itemRanking,
            AddOnRanking = addOnRanking
        });
    }
}
