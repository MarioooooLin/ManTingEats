using System.ComponentModel.DataAnnotations;
using ManTingEats.Models.Validation;

namespace ManTingEats.Models.Commands;

// 加價允許 0，供「加蔥」等免費加料使用
public sealed record CreateAddOnCommand(
    [property: Display(Name = "加料名稱")][Required(ErrorMessage = "請輸入加料名稱")][StringLength(50, ErrorMessage = "名稱不得超過 50 字")] string Name,
    [property: Display(Name = "加價")][Range(typeof(decimal), "0", "10000", ErrorMessage = "加價需介於 0 到 10,000")][WholeAmount] decimal Price);

public sealed record UpdateAddOnCommand(
    int Id,
    [property: Display(Name = "加料名稱")][Required(ErrorMessage = "請輸入加料名稱")][StringLength(50, ErrorMessage = "名稱不得超過 50 字")] string Name,
    [property: Display(Name = "加價")][Range(typeof(decimal), "0", "10000", ErrorMessage = "加價需介於 0 到 10,000")][WholeAmount] decimal Price);
