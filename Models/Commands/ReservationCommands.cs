using System.ComponentModel.DataAnnotations;

namespace ManTingEats.Models.Commands;

public sealed record CreateReservationCommand(
    [Required(ErrorMessage = "請輸入姓名")][StringLength(100, ErrorMessage = "姓名不得超過 100 字")] string CustomerName,
    [Required(ErrorMessage = "請輸入電話")]
    [Phone(ErrorMessage = "電話格式不正確")]
    [StringLength(20, ErrorMessage = "電話不得超過 20 字")]
    string PhoneNumber,
    [Range(1, int.MaxValue, ErrorMessage = "人數需為正整數")] int PartySize,
    [Required(ErrorMessage = "請選擇日期時段")] DateTime ReservedAt);
