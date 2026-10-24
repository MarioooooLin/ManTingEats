namespace ManTingEats.Models.Entities;

/// <summary>
/// 菜單分類（v17）：由店長在菜單管理自行新增、改名、排序、停用，取代原本寫死的「吃／喝／其他」列舉。
/// 只能停用不能刪除，品項一定有分類；停用前須先移走或下架其上架品項，因此上架品項的分類一定是啟用中。
/// </summary>
public sealed class MenuCategory
{
    public int Id { get; set; }
    public required string Name { get; set; }

    /// <summary>點餐頁分類分頁與菜單列表的順序，由小到大；以上移／下移調整。</summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
