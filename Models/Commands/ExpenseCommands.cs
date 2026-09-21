using System.ComponentModel.DataAnnotations;
using ManTingEats.Models.Enums;

namespace ManTingEats.Models.Commands;

public sealed record CreateExpenseCommand(
    [Required(ErrorMessage = "請選擇日期")] DateTime Date,
    [Required(ErrorMessage = "請選擇分類")] ExpenseCategory Category,
    [Range(0.01, double.MaxValue, ErrorMessage = "金額需大於 0")] decimal Amount,
    [StringLength(200, ErrorMessage = "備註不得超過 200 字")] string? Note);

public sealed record UpdateExpenseCommand(
    int Id,
    [Required(ErrorMessage = "請選擇日期")] DateTime Date,
    [Required(ErrorMessage = "請選擇分類")] ExpenseCategory Category,
    [Range(0.01, double.MaxValue, ErrorMessage = "金額需大於 0")] decimal Amount,
    [StringLength(200, ErrorMessage = "備註不得超過 200 字")] string? Note);
