using System.ComponentModel.DataAnnotations;

namespace ManTingEats.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "請輸入帳號")]
    [StringLength(50, ErrorMessage = "帳號不得超過 50 字")]
    [Display(Name = "帳號")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "請輸入密碼")]
    [StringLength(200, ErrorMessage = "密碼不得超過 200 字")]
    [DataType(DataType.Password)]
    [Display(Name = "密碼")]
    public required string Password { get; set; }
}
