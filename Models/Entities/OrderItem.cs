namespace ManTingEats.Models.Entities;

public sealed class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    /// <summary>下單當下的售價快照，避免菜單日後調價影響歷史訂單金額。</summary>
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    /// <summary>給廚房看的單項備註（例：少辣、不要葱），選填。</summary>
    public string? Note { get; set; }

    /// <summary>是否已隨「確認出單」列印過，避免每次加點即時列印造成亂印。</summary>
    public bool IsPrinted { get; set; }
}
