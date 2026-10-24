using ManTingEats.Models.Entities;

namespace ManTingEats.Models;

public sealed class MenuIndexViewModel
{
    public required List<MenuItem> Items { get; init; }
    public required List<AddOn> AddOns { get; init; }
    public required List<MenuCategoryRow> Categories { get; init; }
}

/// <summary>分類分頁的一列（v17）：附品項數，讓店長看出哪些分類停用前要先移走品項。</summary>
public sealed record MenuCategoryRow(MenuCategory Category, int ActiveItemCount, int ItemCount);
