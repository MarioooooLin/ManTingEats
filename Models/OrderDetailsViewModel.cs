using ManTingEats.Models.Entities;

namespace ManTingEats.Models;

public sealed class OrderDetailsViewModel
{
    public required Order Order { get; init; }
    public required List<MenuItem> ActiveMenuItems { get; init; }
    public required List<AddOn> ActiveAddOns { get; init; }

    /// <summary>目前裝置為手機（v14）：送出後不自動出單、不顯示「確認訂單」，改提示到 iPad 出單。</summary>
    public bool IsPhone { get; init; }
}
