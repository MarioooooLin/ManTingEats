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
}
