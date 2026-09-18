using System.ComponentModel.DataAnnotations;
using ManTingEats.Models.Enums;

namespace ManTingEats.Models.Commands;

public sealed record CreateOrderCommand(
    [Required(ErrorMessage = "請選擇通路")] OrderChannel Channel,
    [StringLength(20, ErrorMessage = "桌號不得超過 20 字")] string? TableNumber);

public sealed record AddOrderItemCommand(
    [Range(1, int.MaxValue, ErrorMessage = "請選擇品項")] int MenuItemId,
    [Range(1, int.MaxValue, ErrorMessage = "數量需大於 0")] int Quantity);

public sealed record VoidOrderCommand(
    [Required(ErrorMessage = "請輸入作廢原因")][StringLength(200, ErrorMessage = "原因不得超過 200 字")] string Reason);
