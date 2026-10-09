using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;

namespace ManTingEats.Models;

/// <summary>某營業日即時計算的彙總（尚未存檔）；算法與營收報表一致。</summary>
public sealed record DailyClosingSummary(
    decimal Revenue,
    decimal ExpenseTotal,
    decimal DiscountTotal,
    int CompletedOrderCount,
    int VoidedOrderCount);

public sealed class DailyClosingViewModel
{
    public required DateTime BusinessDate { get; init; }
    public required DailyClosingSummary Summary { get; init; }

    /// <summary>到該營業日為止開立、尚未結帳的訂單；有任何一張就不能日結。</summary>
    public required IReadOnlyList<Order> OpenOrders { get; init; }

    /// <summary>該營業日最後一次日結；null 代表尚未日結。</summary>
    public DailyClosing? LatestClosing { get; init; }

    public required CloseDayCommand Form { get; init; }

    public bool IsReclose => LatestClosing is not null;
}

/// <summary>日結紀錄列表的一列：該營業日最後一次日結與日結次數。</summary>
public sealed record DailyClosingHistoryRow(DailyClosing Latest, int ClosingCount);

public sealed class DailyClosingHistoryViewModel
{
    public required PagedList<DailyClosing> Page { get; init; }
    public required IReadOnlyList<DailyClosingHistoryRow> Rows { get; init; }
}
