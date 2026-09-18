namespace ManTingEats.Models;

public sealed class ReservationSearchViewModel
{
    public string? CustomerName { get; set; }
    public DateTime? Date { get; set; }
    public required List<Entities.Reservation> Results { get; init; }
}
