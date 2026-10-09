using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;

namespace ManTingEats.Models;

public sealed class OrderSearchViewModel
{
    /// <summary>是否帶了查詢條件；false 為預設畫面（今天營業日最新 10 筆）。</summary>
    public bool IsSearch { get; init; }

    /// <summary>營業日區間（06:00 切換，含起訖當天）；留空代表該端不限。</summary>
    public DateTime? Start { get; init; }
    public DateTime? End { get; init; }
    public OrderStatus? Status { get; init; }

    public required PagedList<Order> Results { get; init; }

    /// <summary>所有尚未結帳的訂單張數（含今天與以前營業日），只在預設畫面提示。</summary>
    public int OpenCount { get; init; }
}
