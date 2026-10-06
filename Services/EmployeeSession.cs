using System.Security.Claims;
using ManTingEats.Data;
using ManTingEats.Models.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace ManTingEats.Services;

/// <summary>登入身分的建立與驗證；登入時與店長變更自己密碼後重新簽發 Cookie 都使用同一份 Claims。</summary>
public static class EmployeeSession
{
    public const string SecurityStampClaim = "ManTingEats:SecurityStamp";

    public static ClaimsPrincipal CreatePrincipal(Employee employee)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new(ClaimTypes.Name, employee.Username),
            new(ClaimTypes.Role, employee.Role.ToString()),
            new(SecurityStampClaim, employee.SecurityStamp)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    /// <summary>
    /// 每次請求確認帳號仍啟用、且安全戳記與資料庫一致；停用或重設密碼後，舊 Cookie 立即失效。
    /// 單店使用量小，每次請求多一次以主鍵查詢的成本可忽略，換取「離職員工立即登出」。
    /// </summary>
    public static async Task<bool> IsValidAsync(ClaimsPrincipal principal, AppDbContext db)
    {
        var stamp = principal.FindFirstValue(SecurityStampClaim);
        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var employeeId) || string.IsNullOrEmpty(stamp))
        {
            return false;
        }

        return await db.Employees.AnyAsync(e => e.Id == employeeId && e.IsActive && e.SecurityStamp == stamp);
    }
}
