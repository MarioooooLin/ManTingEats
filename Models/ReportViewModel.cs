using ManTingEats.Models.Enums;

namespace ManTingEats.Models;

public sealed record ChannelRevenue(OrderChannel Channel, decimal Amount);

public sealed record MenuItemSales(string MenuItemName, int Quantity, decimal Amount);

public sealed class ReportViewModel
{
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required decimal TotalRevenue { get; init; }
    public required decimal TotalExpense { get; init; }
    public decimal NetProfit => TotalRevenue - TotalExpense;
    public required List<ChannelRevenue> ChannelBreakdown { get; init; }
    public required List<MenuItemSales> ItemRanking { get; init; }
}
