using System.ComponentModel.DataAnnotations;
using ManTingEats.Models.Enums;
using ManTingEats.Models.Validation;

namespace ManTingEats.Models.Commands;

public sealed record CreateMenuItemCommand(
    [Required(ErrorMessage = "請輸入品項名稱")][StringLength(100, ErrorMessage = "名稱不得超過 100 字")] string Name,
    [Required(ErrorMessage = "請選擇分類")] MenuCategory Category,
    [Range(typeof(decimal), "1", "100000", ErrorMessage = "售價需介於 1 到 100,000")][WholeAmount] decimal Price,
    bool SupportsAddOns = false,
    bool SupportsSpiceLevel = false);

public sealed record UpdateMenuItemCommand(
    int Id,
    [Required(ErrorMessage = "請輸入品項名稱")][StringLength(100, ErrorMessage = "名稱不得超過 100 字")] string Name,
    [Required(ErrorMessage = "請選擇分類")] MenuCategory Category,
    [Range(typeof(decimal), "1", "100000", ErrorMessage = "售價需介於 1 到 100,000")][WholeAmount] decimal Price,
    bool SupportsAddOns = false,
    bool SupportsSpiceLevel = false);
