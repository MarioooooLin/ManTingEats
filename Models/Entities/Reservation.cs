namespace ManTingEats.Models.Entities;

public sealed class Reservation
{
    public int Id { get; set; }
    public required string CustomerName { get; set; }
    public required string PhoneNumber { get; set; }
    public int PartySize { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
