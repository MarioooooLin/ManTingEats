using System.Security.Claims;
using ManTingEats.Data;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using ManTingEats.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManTingEats.Controllers;

/// <summary>
/// 帳號管理（v7，店長限定）。系統只有一個店長：只能新增員工，店長帳號不能停用或由此重設，
/// 店長改密碼走 ChangePassword（需輸入目前密碼）。
/// </summary>
[Authorize(Roles = AppRoles.Manager)]
public sealed class EmployeeController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Employee> _passwordHasher;

    public EmployeeController(AppDbContext db, IPasswordHasher<Employee> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    private int CurrentEmployeeId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        var employees = await _db.Employees
            .OrderBy(e => e.Role).ThenBy(e => e.Username)
            .ToListAsync();
        return View(employees);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEmployeeCommand command)
    {
        var username = command.Username.Trim();
        if (ModelState.IsValid && await UsernameExistsAsync(username))
        {
            ModelState.AddModelError(nameof(command.Username), "此帳號已存在。");
        }

        if (!ModelState.IsValid)
        {
            return View(command);
        }

        var employee = new Employee
        {
            Username = username,
            PasswordHash = string.Empty,
            Role = EmployeeRole.Staff
        };
        employee.PasswordHash = _passwordHasher.HashPassword(employee, command.Password);
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"已新增員工帳號「{employee.Username}」。";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>MySQL 預設定序不分大小寫，比對時統一轉小寫，讓測試用的 SQLite 也得到相同結果。</summary>
    private Task<bool> UsernameExistsAsync(string username)
    {
        var normalized = username.ToLower();
        return _db.Employees.AnyAsync(e => e.Username.ToLower() == normalized);
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var employee = await FindStaffAsync(id);
        if (employee is null)
        {
            return RedirectToAction(nameof(Index));
        }

        ViewData["Username"] = employee.Username;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordCommand command)
    {
        var employee = await FindStaffAsync(id);
        if (employee is null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            ViewData["Username"] = employee.Username;
            return View(command);
        }

        employee.PasswordHash = _passwordHasher.HashPassword(employee, command.NewPassword);
        // 換新戳記：該員工已登入的裝置會被登出，需用新密碼重新登入
        employee.SecurityStamp = Employee.NewSecurityStamp();
        await _db.SaveChangesAsync();

        TempData["Success"] = $"已重設「{employee.Username}」的密碼，該帳號已登入的裝置需重新登入。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var employee = await FindStaffAsync(id);
        if (employee is null)
        {
            return RedirectToAction(nameof(Index));
        }

        employee.IsActive = !employee.IsActive;
        if (!employee.IsActive)
        {
            employee.SecurityStamp = Employee.NewSecurityStamp();
        }
        await _db.SaveChangesAsync();

        TempData["Success"] = employee.IsActive
            ? $"已啟用「{employee.Username}」。"
            : $"已停用「{employee.Username}」，該帳號已登入的裝置會被登出。";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>只允許操作員工帳號；店長帳號或不存在的帳號一律帶錯誤訊息回列表。</summary>
    private async Task<Employee?> FindStaffAsync(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null)
        {
            TempData["Error"] = "找不到此帳號。";
            return null;
        }

        if (employee.Role != EmployeeRole.Staff)
        {
            TempData["Error"] = "店長帳號不能在此停用或重設密碼，請使用「變更我的密碼」。";
            return null;
        }

        return employee;
    }

    [HttpGet]
    public IActionResult ChangePassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command)
    {
        var employee = await _db.Employees.FindAsync(CurrentEmployeeId);
        if (employee is null)
        {
            return NotFound();
        }

        if (ModelState.IsValid
            && _passwordHasher.VerifyHashedPassword(employee, employee.PasswordHash, command.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(command.CurrentPassword), "目前密碼不正確。");
        }

        if (!ModelState.IsValid)
        {
            return View(command);
        }

        employee.PasswordHash = _passwordHasher.HashPassword(employee, command.NewPassword);
        employee.SecurityStamp = Employee.NewSecurityStamp();
        await _db.SaveChangesAsync();

        // 戳記已換新，用新的戳記重新簽發目前裝置的登入 Cookie，避免改完密碼自己也被登出；其他裝置則會被登出
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, EmployeeSession.CreatePrincipal(employee));

        TempData["Success"] = "密碼已變更，其他已登入的裝置需用新密碼重新登入。";
        return RedirectToAction(nameof(Index));
    }
}
