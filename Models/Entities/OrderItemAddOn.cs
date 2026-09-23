namespace ManTingEats.Models.Entities;

public sealed class OrderItemAddOn
{
    public int Id { get; set; }

    public int OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    public int AddOnId { get; set; }
    public AddOn? AddOn { get; set; }

    /// <summary>下單當下的加料名稱/售價快照，避免加料日後改名或調價影響歷史訂單金額。</summary>
    public required string AddOnName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
