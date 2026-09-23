namespace ManTingEats.Models.Entities;

/// <summary>全店共用加料清單，非品項專屬。</summary>
public sealed class AddOn
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<OrderItemAddOn> OrderItemAddOns { get; set; } = new List<OrderItemAddOn>();
}
