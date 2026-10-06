using System.ComponentModel.DataAnnotations;
using ManTingEats.Models.Enums;
using ManTingEats.Models.Validation;

namespace ManTingEats.Models.Commands;

public sealed record CreateExpenseCommand(
    [property: Display(Name = "日期")][Required(ErrorMessage = "請選擇日期")] DateTime Date,
    [property: Display(Name = "分類")][Required(ErrorMessage = "請選擇分類")] ExpenseCategory Category,
    [property: Display(Name = "金額")][Range(typeof(decimal), "1", "10000000", ErrorMessage = "金額需介於 1 到 10,000,000")][WholeAmount] decimal Amount,
    [property: Display(Name = "備註")][StringLength(200, ErrorMessage = "備註不得超過 200 字")] string? Note);

public sealed record UpdateExpenseCommand(
    int Id,
    [property: Display(Name = "日期")][Required(ErrorMessage = "請選擇日期")] DateTime Date,
    [property: Display(Name = "分類")][Required(ErrorMessage = "請選擇分類")] ExpenseCategory Category,
    [property: Display(Name = "金額")][Range(typeof(decimal), "1", "10000000", ErrorMessage = "金額需介於 1 到 10,000,000")][WholeAmount] decimal Amount,
    [property: Display(Name = "備註")][StringLength(200, ErrorMessage = "備註不得超過 200 字")] string? Note);
