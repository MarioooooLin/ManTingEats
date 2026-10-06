using ManTingEats.Models.Enums;

namespace ManTingEats.Models;

public sealed record ChannelRevenue(OrderChannel Channel, decimal Amount);

public sealed record MenuItemSales(string MenuItemName, int Quantity, decimal Amount);

public sealed record AddOnSales(string AddOnName, int Quantity, decimal Amount);

public sealed class ReportViewModel
{
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    /// <summary>實收合計（原價 − 折扣）。</summary>
    public required decimal TotalRevenue { get; init; }
    /// <summary>本期折扣總額（v8）；品項＋加料排行 − 折扣總額 = 總營收。</summary>
    public decimal TotalDiscount { get; init; }
    public required decimal TotalExpense { get; init; }
    public decimal NetProfit => TotalRevenue - TotalExpense;
    public required List<ChannelRevenue> ChannelBreakdown { get; init; }
    /// <summary>品項本身的銷售（原價，不含加料），加料另列於 <see cref="AddOnRanking"/>；兩者相加減去折扣總額即為總營收。</summary>
    public required List<MenuItemSales> ItemRanking { get; init; }
    public required List<AddOnSales> AddOnRanking { get; init; }
}
