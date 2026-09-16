namespace ManTingEats.Models.Entities;

public sealed class MenuItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
