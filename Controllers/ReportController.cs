using ManTingEats.Data;
using ManTingEats.Models;
using ManTingEats.Models.Enums;
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
        var startDate = (start ?? DateTime.Today).Date;
        var endDate = (end ?? DateTime.Today).Date;
        if (endDate < startDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        var endExclusive = endDate.AddDays(1);

        // 報表僅計入已結帳訂單，不含已作廢訂單
        var completedOrders = await _db.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= startDate && o.CompletedAt < endExclusive)
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
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

        return View(new ReportViewModel
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalRevenue = totalRevenue,
            TotalExpense = totalExpense,
            ChannelBreakdown = channelBreakdown,
            ItemRanking = itemRanking
        });
    }
}
