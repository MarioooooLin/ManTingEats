namespace ManTingEats.Models;

public sealed class ReservationSearchViewModel
{
    public string? CustomerName { get; set; }
    public DateTime? Date { get; set; }
    /// <summary>未指定姓名與日期時，只列出今天以後的訂位。</summary>
    public bool UpcomingOnly { get; init; }
    public required PagedList<Entities.Reservation> Results { get; init; }
}
