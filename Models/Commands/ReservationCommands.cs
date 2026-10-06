using System.ComponentModel.DataAnnotations;

namespace ManTingEats.Models.Commands;

public sealed record CreateReservationCommand(
    [property: Display(Name = "姓名")][Required(ErrorMessage = "請輸入姓名")][StringLength(100, ErrorMessage = "姓名不得超過 100 字")] string CustomerName,
    [property: Display(Name = "電話")][Required(ErrorMessage = "請輸入電話")]
    [Phone(ErrorMessage = "電話格式不正確")]
    [StringLength(20, ErrorMessage = "電話不得超過 20 字")]
    string PhoneNumber,
    [property: Display(Name = "人數")][Range(1, 50, ErrorMessage = "人數需介於 1 到 50")] int PartySize,
    [property: Display(Name = "訂位時段")][Required(ErrorMessage = "請選擇日期時段")] DateTime ReservedAt);

public sealed record UpdateReservationCommand(
    int Id,
    [property: Display(Name = "姓名")][Required(ErrorMessage = "請輸入姓名")][StringLength(100, ErrorMessage = "姓名不得超過 100 字")] string CustomerName,
    [property: Display(Name = "電話")][Required(ErrorMessage = "請輸入電話")]
    [Phone(ErrorMessage = "電話格式不正確")]
    [StringLength(20, ErrorMessage = "電話不得超過 20 字")]
    string PhoneNumber,
    [property: Display(Name = "人數")][Range(1, 50, ErrorMessage = "人數需介於 1 到 50")] int PartySize,
    [property: Display(Name = "訂位時段")][Required(ErrorMessage = "請選擇日期時段")] DateTime ReservedAt);
