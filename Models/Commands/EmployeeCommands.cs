using System.ComponentModel.DataAnnotations;

namespace ManTingEats.Models.Commands;

/// <summary>帳號與密碼長度限制（v7）；資料庫 Username 欄位上限為 50。</summary>
public static class AccountLimits
{
    public const int MinUsernameLength = 3;
    public const int MaxUsernameLength = 50;
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 100;
}

// 以下表單需要 [Compare] 檢查兩次密碼一致；[Compare] 只能掛在屬性上，而 MVC 不接受 positional record 的屬性驗證，
// 因此改用一般 class（同 LoginViewModel 的寫法），不沿用其他 Command 的 record 形式。

public sealed class CreateEmployeeCommand
{
    [Display(Name = "帳號")]
    [Required(ErrorMessage = "請輸入帳號")]
    [StringLength(AccountLimits.MaxUsernameLength, MinimumLength = AccountLimits.MinUsernameLength, ErrorMessage = "帳號需為 {2} 到 {1} 字")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "密碼")]
    [Required(ErrorMessage = "請輸入密碼")]
    [StringLength(AccountLimits.MaxPasswordLength, MinimumLength = AccountLimits.MinPasswordLength, ErrorMessage = "密碼需為 {2} 到 {1} 碼")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "確認密碼")]
    [Required(ErrorMessage = "請再輸入一次密碼")]
    [Compare(nameof(Password), ErrorMessage = "兩次輸入的密碼不一致")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ResetPasswordCommand
{
    [Display(Name = "新密碼")]
    [Required(ErrorMessage = "請輸入新密碼")]
    [StringLength(AccountLimits.MaxPasswordLength, MinimumLength = AccountLimits.MinPasswordLength, ErrorMessage = "密碼需為 {2} 到 {1} 碼")]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "確認新密碼")]
    [Required(ErrorMessage = "請再輸入一次新密碼")]
    [Compare(nameof(NewPassword), ErrorMessage = "兩次輸入的密碼不一致")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public sealed class ChangePasswordCommand
{
    [Display(Name = "目前密碼")]
    [Required(ErrorMessage = "請輸入目前密碼")]
    [StringLength(200, ErrorMessage = "密碼不得超過 200 字")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Display(Name = "新密碼")]
    [Required(ErrorMessage = "請輸入新密碼")]
    [StringLength(AccountLimits.MaxPasswordLength, MinimumLength = AccountLimits.MinPasswordLength, ErrorMessage = "密碼需為 {2} 到 {1} 碼")]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "確認新密碼")]
    [Required(ErrorMessage = "請再輸入一次新密碼")]
    [Compare(nameof(NewPassword), ErrorMessage = "兩次輸入的密碼不一致")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
