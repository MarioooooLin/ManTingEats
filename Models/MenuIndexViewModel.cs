using ManTingEats.Models.Entities;

namespace ManTingEats.Models;

public sealed class MenuIndexViewModel
{
    public required List<MenuItem> Items { get; init; }
    public required List<AddOn> AddOns { get; init; }
}
