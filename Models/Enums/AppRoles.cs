namespace ManTingEats.Models.Enums;

/// <summary>[Authorize(Roles = ...)] 需要字串常數；與登入時寫入 Cookie 的角色（EmployeeRole.ToString()）一致。</summary>
public static class AppRoles
{
    public const string Manager = nameof(EmployeeRole.Manager);
}
