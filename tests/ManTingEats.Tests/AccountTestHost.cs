using System.Security.Claims;
using ManTingEats.Controllers;
using ManTingEats.Data;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace ManTingEats.Tests;

/// <summary>
/// 帳號相關測試的環境：SQLite 記憶體資料庫，預先建立一個店長與一個員工（密碼皆為 InitialPassword），
/// 並以假的驗證服務記錄 SignIn，讓測試能檢查重新簽發的 Cookie 內容。
/// </summary>
public sealed class AccountTestHost : IDisposable
{
    public const string InitialPassword = "initial-pass";
    public const int ManagerId = 1;
    public const int StaffId = 2;

    private readonly SqliteConnection _connection;
    public PasswordHasher<Employee> Hasher { get; } = new();
    public RecordingAuthenticationService Auth { get; } = new();

    public AccountTestHost()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var db = CreateDbContext();
        db.Database.EnsureCreated();
        db.Employees.AddRange(
            NewEmployee(ManagerId, "boss", EmployeeRole.Manager),
            NewEmployee(StaffId, "staff1", EmployeeRole.Staff));
        db.SaveChanges();
    }

    private Employee NewEmployee(int id, string username, EmployeeRole role)
    {
        var employee = new Employee { Id = id, Username = username, PasswordHash = string.Empty, Role = role };
        employee.PasswordHash = Hasher.HashPassword(employee, InitialPassword);
        return employee;
    }

    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public Employee Load(int id)
    {
        using var db = CreateDbContext();
        return db.Employees.AsNoTracking().Single(e => e.Id == id);
    }

    public bool PasswordMatches(int id, string password)
    {
        var employee = Load(id);
        return Hasher.VerifyHashedPassword(employee, employee.PasswordHash, password) != PasswordVerificationResult.Failed;
    }

    public EmployeeController CreateEmployeeController(int signedInAs = ManagerId) =>
        Configure(new EmployeeController(CreateDbContext(), Hasher), signedInAs);

    public AccountController CreateAccountController() =>
        Configure(new AccountController(CreateDbContext(), Hasher, new MemoryCache(new MemoryCacheOptions())), signedInAs: null);

    private T Configure<T>(T controller, int? signedInAs) where T : Controller
    {
        var services = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(Auth)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        if (signedInAs is int id)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, id.ToString())], "Test"));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new NullTempDataProvider());
        controller.Url = new LocalUrlHelper();
        return controller;
    }

    public void Dispose() => _connection.Dispose();

    /// <summary>只記錄最後一次 SignIn 的身分，其餘動作不做事。</summary>
    public sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public ClaimsPrincipal? SignedIn { get; private set; }

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            SignedIn = principal;
            return Task.CompletedTask;
        }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(AuthenticateResult.NoResult());
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    /// <summary>登入成功後的導向只需判斷 returnUrl 是否為站內網址；測試一律不帶 returnUrl。</summary>
    private sealed class LocalUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new();
        public string? Action(UrlActionContext actionContext) => null;
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => !string.IsNullOrEmpty(url) && url.StartsWith('/');
        public string? Link(string? routeName, object? values) => null;
        public string? RouteUrl(UrlRouteContext routeContext) => null;
    }
}
