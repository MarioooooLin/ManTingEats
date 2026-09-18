using System.ComponentModel.DataAnnotations;

namespace ManTingEats.Models.Commands;

public sealed record CreateMenuItemCommand(
    [Required(ErrorMessage = "請輸入品項名稱")][StringLength(100, ErrorMessage = "名稱不得超過 100 字")] string Name,
    [Required(ErrorMessage = "請輸入分類")][StringLength(50, ErrorMessage = "分類不得超過 50 字")] string Category,
    [Range(0.01, double.MaxValue, ErrorMessage = "售價需大於 0")] decimal Price);

public sealed record UpdateMenuItemCommand(
    int Id,
    [Required(ErrorMessage = "請輸入品項名稱")][StringLength(100, ErrorMessage = "名稱不得超過 100 字")] string Name,
    [Required(ErrorMessage = "請輸入分類")][StringLength(50, ErrorMessage = "分類不得超過 50 字")] string Category,
    [Range(0.01, double.MaxValue, ErrorMessage = "售價需大於 0")] decimal Price);
