using ManTingEats.Models.Enums;

namespace ManTingEats.Models.Entities;

public sealed class Employee
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public EmployeeRole Role { get; set; } = EmployeeRole.Manager;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>停用後無法登入；帳號不提供刪除，因歷史訂單記錄了開單與作廢的操作者。</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 安全戳記：密碼變更或停用時換新，登入 Cookie 中的值與此不符即強制登出，
    /// 讓重設密碼或停用立即對所有已登入的裝置生效。
    /// </summary>
    public string SecurityStamp { get; set; } = NewSecurityStamp();

    public static string NewSecurityStamp() => Guid.NewGuid().ToString("N");

    public ICollection<Order> CreatedOrders { get; set; } = new List<Order>();
}
