using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;

namespace ManTingEats.Models;

public sealed class ExpenseSearchViewModel
{
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public ExpenseCategory? Category { get; set; }
    public required List<Expense> Results { get; init; }
    public decimal TotalAmount { get; init; }
}
