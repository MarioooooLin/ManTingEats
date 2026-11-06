using ManTingEats.Models.Enums;

namespace ManTingEats.Models;

public sealed record ChannelRevenue(OrderChannel Channel, decimal Amount, int OrderCount = 0);

public sealed record MenuItemSales(string MenuItemName, int Quantity, decimal Amount);

public sealed record AddOnSales(string AddOnName, int Quantity, decimal Amount);

/// <summary>外送平台合計（v18），來自每個營業日最後一次日結手動填寫的數字。</summary>
public sealed record DeliveryRevenue(string Platform, int OrderCount, decimal Sales, decimal Payout);

public sealed class ReportViewModel
{
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    /// <summary>店內（內用＋外帶）實收合計（原價 − 折扣），不含外送。</summary>
    public required decimal TotalRevenue { get; init; }
    /// <summary>本期折扣總額（v8）；品項＋加料排行 − 折扣總額 = 店內營收。</summary>
    public decimal TotalDiscount { get; init; }
    public required decimal TotalExpense { get; init; }
    public List<DeliveryRevenue> DeliveryBreakdown { get; init; } = [];
    /// <summary>區間內（不含未來）尚未日結的營業日數；這些天的外送數字還沒填，不在報表內。</summary>
    public int UnclosedDayCount { get; init; }
    public decimal DeliverySales => DeliveryBreakdown.Sum(d => d.Sales);
    public decimal DeliveryPayout => DeliveryBreakdown.Sum(d => d.Payout);
    /// <summary>總營業額 = 店內營收 ＋ 外送營業額（v18）。</summary>
    public decimal TotalSales => TotalRevenue + DeliverySales;
    /// <summary>淨利以外送實收（扣平台抽成後實際入帳）計算（v18）。</summary>
    public decimal NetProfit => TotalRevenue + DeliveryPayout - TotalExpense;
    public required List<ChannelRevenue> ChannelBreakdown { get; init; }
    /// <summary>品項本身的銷售（原價，不含加料），加料另列於 <see cref="AddOnRanking"/>；兩者相加減去折扣總額即為總營收。</summary>
    public required List<MenuItemSales> ItemRanking { get; init; }
    public required List<AddOnSales> AddOnRanking { get; init; }
}
