using System.ComponentModel.DataAnnotations;

namespace ManTingEats.Models.Commands;

public sealed record CreateMenuCategoryCommand(
    [property: Display(Name = "分類名稱")][Required(ErrorMessage = "請輸入分類名稱")][StringLength(20, ErrorMessage = "名稱不得超過 20 字")] string Name);

public sealed record UpdateMenuCategoryCommand(
    int Id,
    [property: Display(Name = "分類名稱")][Required(ErrorMessage = "請輸入分類名稱")][StringLength(20, ErrorMessage = "名稱不得超過 20 字")] string Name);
