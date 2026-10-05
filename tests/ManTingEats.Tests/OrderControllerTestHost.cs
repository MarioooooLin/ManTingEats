using System.Security.Claims;
using ManTingEats.Controllers;
using ManTingEats.Data;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ManTingEats.Tests;

/// <summary>
/// OrderController 測試用的環境：SQLite 記憶體資料庫（會檢查外鍵與連動刪除，比 EF InMemory 更接近 MySQL 的行為）
/// 加上已登入的使用者、TempData 與網址產生器。每個測試各建一個，彼此資料不共用。
/// </summary>
public sealed class OrderControllerTestHost : IDisposable
{
    public const int EmployeeId = 1;
    public const int SimpleItemId = 1;      // 一般品項：$100，不可客製化
    public const int CustomItemId = 2;      // 客製化品項：$120，可加料、可調辣度
    public const int InactiveItemId = 3;    // 已下架品項
    public const int EggAddOnId = 1;        // 加料：$10
    public const int InactiveAddOnId = 2;   // 已停用加料

    private readonly SqliteConnection _connection;

    public OrderControllerTestHost()
    {
        // 記憶體資料庫只在連線開著時存在，因此整個測試共用同一條連線
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var db = CreateDbContext();
        db.Database.EnsureCreated();
        db.Employees.Add(new Employee { Id = EmployeeId, Username = "admin", PasswordHash = "x" });
        db.MenuItems.AddRange(
            new MenuItem { Id = SimpleItemId, Name = "酸梅湯", Price = 100m, Category = MenuCategory.Drink },
            new MenuItem { Id = CustomItemId, Name = "爆辣冷麵", Price = 120m, SupportsAddOns = true, SupportsSpiceLevel = true },
            new MenuItem { Id = InactiveItemId, Name = "停賣品", Price = 50m, IsActive = false });
        db.AddOns.AddRange(
            new AddOn { Id = EggAddOnId, Name = "加蛋", Price = 10m },
            new AddOn { Id = InactiveAddOnId, Name = "停用加料", Price = 20m, IsActive = false });
        db.SaveChanges();
    }

    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    /// <summary>模擬一次獨立的 HTTP 請求：每次都用新的 DbContext，避免前一個動作追蹤中的實體影響結果。</summary>
    public OrderController CreateController()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, EmployeeId.ToString())], "Test"))
        };
        return new OrderController(CreateDbContext(), NullLogger<OrderController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new NullTempDataProvider()),
            Url = new FakeUrlHelper()
        };
    }

    /// <summary>直接在資料庫建立一張訂單，讓測試聚焦在要驗證的動作上。</summary>
    public int SeedOrder(OrderStatus status = OrderStatus.Open, params OrderItem[] items)
    {
        using var db = CreateDbContext();
        var order = new Order
        {
            Channel = OrderChannel.Takeout,
            Status = status,
            DailyNumber = 1,
            CreatedByEmployeeId = EmployeeId
        };
        foreach (var item in items)
        {
            order.Items.Add(item);
        }
        order.TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity + i.AddOns.Sum(a => a.UnitPrice * a.Quantity));
        db.Orders.Add(order);
        db.SaveChanges();
        return order.Id;
    }

    public static OrderItem Item(int quantity = 1, bool isPrinted = false, int menuItemId = SimpleItemId, decimal unitPrice = 100m) =>
        new() { MenuItemId = menuItemId, UnitPrice = unitPrice, Quantity = quantity, IsPrinted = isPrinted };

    public Order LoadOrder(int id)
    {
        using var db = CreateDbContext();
        return db.Orders
            .Include(o => o.Items).ThenInclude(i => i.AddOns)
            .AsNoTracking()
            .Single(o => o.Id == id);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    /// <summary>只需產生 PassPRNT 回呼路徑，以「/Order/動作?參數」的形式回傳，方便斷言內容。</summary>
    private sealed class FakeUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new();

        public string? Action(UrlActionContext actionContext)
        {
            var values = new RouteValueDictionary(actionContext.Values);
            var query = string.Join("&", values.Select(v => $"{v.Key}={v.Value}"));
            return $"/Order/{actionContext.Action}?{query}";
        }

        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => true;
        public string? Link(string? routeName, object? values) => null;
        public string? RouteUrl(UrlRouteContext routeContext) => null;
    }
}
