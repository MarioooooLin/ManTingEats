using ManTingEats.Models.Enums;

namespace ManTingEats.Models.Entities;

public sealed class Expense
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public ExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
