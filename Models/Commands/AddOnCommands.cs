using System.ComponentModel.DataAnnotations;

namespace ManTingEats.Models.Commands;

public sealed record CreateAddOnCommand(
    [Required(ErrorMessage = "請輸入加料名稱")][StringLength(50, ErrorMessage = "名稱不得超過 50 字")] string Name,
    [Range(0.01, double.MaxValue, ErrorMessage = "加價需大於 0")] decimal Price);

public sealed record UpdateAddOnCommand(
    int Id,
    [Required(ErrorMessage = "請輸入加料名稱")][StringLength(50, ErrorMessage = "名稱不得超過 50 字")] string Name,
    [Range(0.01, double.MaxValue, ErrorMessage = "加價需大於 0")] decimal Price);
