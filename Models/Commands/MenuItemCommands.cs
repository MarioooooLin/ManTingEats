using System.ComponentModel.DataAnnotations;
using ManTingEats.Models.Enums;

namespace ManTingEats.Models.Commands;

public sealed record CreateMenuItemCommand(
    [Required(ErrorMessage = "請輸入品項名稱")][StringLength(100, ErrorMessage = "名稱不得超過 100 字")] string Name,
    [Required(ErrorMessage = "請選擇分類")] MenuCategory Category,
    [Range(0.01, double.MaxValue, ErrorMessage = "售價需大於 0")] decimal Price,
    bool SupportsAddOns = false,
    bool SupportsSpiceLevel = false);

public sealed record UpdateMenuItemCommand(
    int Id,
    [Required(ErrorMessage = "請輸入品項名稱")][StringLength(100, ErrorMessage = "名稱不得超過 100 字")] string Name,
    [Required(ErrorMessage = "請選擇分類")] MenuCategory Category,
    [Range(0.01, double.MaxValue, ErrorMessage = "售價需大於 0")] decimal Price,
    bool SupportsAddOns = false,
    bool SupportsSpiceLevel = false);
