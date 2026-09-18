using ManTingEats.Models.Entities;

namespace ManTingEats.Models;

public sealed class OrderDetailsViewModel
{
    public required Order Order { get; init; }
    public required List<MenuItem> ActiveMenuItems { get; init; }
}
