using System.ComponentModel.DataAnnotations;
using ManTingEats.Models.Enums;

namespace ManTingEats.Models.Commands;

public static class OrderLimits
{
    /// <summary>單行品項／加料數量上限；避免極端值使金額超出資料庫 decimal(10,2) 範圍導致存檔失敗。</summary>
    public const int MaxQuantity = 99;

    /// <summary>辣度為 0（不辣）到 6 的數字，出單直接印數字；未選擇時預設 1（2026-10-05 使用者決議，取代原本五級中文辣度）。</summary>
    public const int MinSpiceLevel = 0;
    public const int MaxSpiceLevel = 6;
    public const int DefaultSpiceLevel = 1;
}

public sealed record CreateOrderCommand(
    [Required(ErrorMessage = "請選擇通路")] OrderChannel Channel,
    [StringLength(20, ErrorMessage = "桌號不得超過 20 字")] string? TableNumber);

public sealed record AddOnSelectionCommand(
    [Range(1, int.MaxValue, ErrorMessage = "加料品項錯誤")] int AddOnId,
    [Range(1, OrderLimits.MaxQuantity, ErrorMessage = "加料數量需介於 1 到 99")] int Quantity);

public sealed record AddOrderItemCommand(
    [Range(1, int.MaxValue, ErrorMessage = "請選擇品項")] int MenuItemId,
    [Range(1, OrderLimits.MaxQuantity, ErrorMessage = "數量需介於 1 到 99")] int Quantity,
    [StringLength(100, ErrorMessage = "備註不得超過 100 字")] string? Note = null,
    [Range(OrderLimits.MinSpiceLevel, OrderLimits.MaxSpiceLevel, ErrorMessage = "辣度需介於 0 到 6")] int? SpiceLevel = null,
    List<AddOnSelectionCommand>? AddOns = null);

public sealed record VoidOrderCommand(
    [Required(ErrorMessage = "請輸入作廢原因")][StringLength(200, ErrorMessage = "原因不得超過 200 字")] string Reason);
