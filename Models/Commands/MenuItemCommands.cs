using System.ComponentModel.DataAnnotations;
using ManTingEats.Models.Enums;
using ManTingEats.Models.Validation;

namespace ManTingEats.Models.Commands;

public sealed record CreateMenuItemCommand(
    [property: Display(Name = "品項名稱")][Required(ErrorMessage = "請輸入品項名稱")][StringLength(100, ErrorMessage = "名稱不得超過 100 字")] string Name,
    [property: Display(Name = "分類")][Required(ErrorMessage = "請選擇分類")] MenuCategory Category,
    [property: Display(Name = "售價")][Range(typeof(decimal), "1", "100000", ErrorMessage = "售價需介於 1 到 100,000")][WholeAmount] decimal Price,
    [property: Display(Name = "開放加料")] bool SupportsAddOns = false,
    [property: Display(Name = "開放調整辣度")] bool SupportsSpiceLevel = false);

public sealed record UpdateMenuItemCommand(
    int Id,
    [property: Display(Name = "品項名稱")][Required(ErrorMessage = "請輸入品項名稱")][StringLength(100, ErrorMessage = "名稱不得超過 100 字")] string Name,
    [property: Display(Name = "分類")][Required(ErrorMessage = "請選擇分類")] MenuCategory Category,
    [property: Display(Name = "售價")][Range(typeof(decimal), "1", "100000", ErrorMessage = "售價需介於 1 到 100,000")][WholeAmount] decimal Price,
    [property: Display(Name = "開放加料")] bool SupportsAddOns = false,
    [property: Display(Name = "開放調整辣度")] bool SupportsSpiceLevel = false);
