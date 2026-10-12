using System.ComponentModel.DataAnnotations;
using ManTingEats.Models.Enums;

namespace ManTingEats.Models.Commands;

public static class OrderLimits
{
    /// <summary>單行品項／加料數量上限；避免極端值使金額超出資料庫 decimal(10,2) 範圍導致存檔失敗。</summary>
    public const int MaxQuantity = 99;

    /// <summary>桌號長度上限（2026-10-05 使用者決議只限制長度、中英數皆可）；資料庫欄位為 20，在此範圍內調整不需 Migration。</summary>
    public const int MaxTableNumberLength = 10;

    /// <summary>辣度為 0（不辣）到 6 的數字，出單直接印數字；未選擇時預設 1（2026-10-05 使用者決議，取代原本五級中文辣度）。</summary>
    public const int MinSpiceLevel = 0;
    public const int MaxSpiceLevel = 6;
    public const int DefaultSpiceLevel = 1;

    /// <summary>
    /// 待出單品項上限（v14，2026-10-13 使用者決議）：出單內容放在網址交給 PassPRNT，過長可能列印失敗。
    /// 以「列」計（滷肉飯 x5 算 1 項），已出單的不計；確切上限未實測，實測後可調整此值。
    /// </summary>
    public const int MaxPendingItems = 20;
}

public sealed record CreateOrderCommand(
    [property: Display(Name = "通路")][Required(ErrorMessage = "請選擇通路")] OrderChannel Channel,
    [StringLength(OrderLimits.MaxTableNumberLength, ErrorMessage = "桌號最多 {1} 個字")] string? TableNumber);

public sealed record AddOnSelectionCommand(
    [Range(1, int.MaxValue, ErrorMessage = "加料品項錯誤")] int AddOnId,
    [Range(1, OrderLimits.MaxQuantity, ErrorMessage = "加料數量需介於 1 到 99")] int Quantity);

public sealed record AddOrderItemCommand(
    [Range(1, int.MaxValue, ErrorMessage = "請選擇品項")] int MenuItemId,
    [Range(1, OrderLimits.MaxQuantity, ErrorMessage = "數量需介於 1 到 99")] int Quantity,
    [StringLength(100, ErrorMessage = "備註不得超過 100 字")] string? Note = null,
    [Range(OrderLimits.MinSpiceLevel, OrderLimits.MaxSpiceLevel, ErrorMessage = "辣度需介於 0 到 6")] int? SpiceLevel = null,
    List<AddOnSelectionCommand>? AddOns = null);

/// <summary>整批點餐（v14）：畫面上的「本次點餐」清單一次送出，全部成功或全部不寫入。</summary>
public sealed record AddOrderItemsCommand(
    [Required(ErrorMessage = "請先選擇品項")][MinLength(1, ErrorMessage = "請先選擇品項")] List<AddOrderItemCommand> Items);

public sealed record VoidOrderCommand(
    [Required(ErrorMessage = "請輸入作廢原因")][StringLength(200, ErrorMessage = "原因不得超過 200 字")] string Reason);
