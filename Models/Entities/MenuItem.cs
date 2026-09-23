using ManTingEats.Models.Enums;

namespace ManTingEats.Models.Entities;

public sealed class MenuItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public MenuCategory Category { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public bool SupportsAddOns { get; set; }
    public bool SupportsSpiceLevel { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
