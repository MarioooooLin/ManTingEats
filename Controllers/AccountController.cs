using System.Security.Claims;
using ManTingEats.Data;
using ManTingEats.Models;
using ManTingEats.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ManTingEats.Controllers;

public sealed class AccountController : Controller
{
    public const string LoginRateLimitPolicy = "login";

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Employee> _passwordHasher;
    private readonly IMemoryCache _cache;

    public AccountController(AppDbContext db, IPasswordHasher<Employee> passwordHasher, IMemoryCache cache)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _cache = cache;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(LoginRateLimitPolicy)]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // 帳號比對在 MySQL 為不分大小寫，key 需正規化避免以大小寫變化繞過次數限制；
        // 並綁定來源 IP，避免外部攻擊者故意輸錯密碼把店內管理者帳號鎖住（總嘗試次數另由 IP 限流把關）
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var lockoutKey = $"login-lockout:{model.Username.Trim().ToUpperInvariant()}:{clientIp}";
        if (_cache.TryGetValue<int>(lockoutKey, out var failedCount) && failedCount >= MaxFailedAttempts)
        {
            ModelState.AddModelError(string.Empty, "登入嘗試次數過多，請 15 分鐘後再試。");
            return View(model);
        }

        var employee = await _db.Employees.SingleOrDefaultAsync(e => e.Username == model.Username);
        // 帳號不存在與密碼錯誤回傳同一則訊息，避免洩漏帳號是否存在
        if (employee is null || _passwordHasher.VerifyHashedPassword(employee, employee.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
        {
            _cache.Set(lockoutKey, failedCount + 1, LockoutDuration);
            ModelState.AddModelError(string.Empty, "帳號或密碼錯誤。");
            return View(model);
        }

        _cache.Remove(lockoutKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new(ClaimTypes.Name, employee.Username),
            new(ClaimTypes.Role, employee.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
