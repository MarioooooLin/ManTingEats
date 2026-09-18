using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ManTingEats.Data;

/// <summary>應用程式啟動時的種子資料邏輯。</summary>
public static class DbSeeder
{
    public static async Task SeedManagerAsync(AppDbContext db, IPasswordHasher<Employee> passwordHasher, IConfiguration configuration)
    {
        if (await db.Employees.AnyAsync())
        {
            return;
        }

        var username = configuration["SeedAdmin:Username"];
        var password = configuration["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            // 未提供種子帳密設定時略過，避免用固定預設密碼建立正式環境帳號
            return;
        }

        var employee = new Employee
        {
            Username = username,
            PasswordHash = string.Empty,
            Role = EmployeeRole.Manager
        };
        employee.PasswordHash = passwordHasher.HashPassword(employee, password);

        db.Employees.Add(employee);
        await db.SaveChangesAsync();
    }
}
