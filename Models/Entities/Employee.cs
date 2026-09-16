using ManTingEats.Models.Enums;

namespace ManTingEats.Models.Entities;

public sealed class Employee
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public EmployeeRole Role { get; set; } = EmployeeRole.Manager;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Order> CreatedOrders { get; set; } = new List<Order>();
}
