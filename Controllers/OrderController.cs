using System.Security.Claims;
using ManTingEats.Data;
using ManTingEats.Models;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using ManTingEats.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManTingEats.Controllers;

[Authorize]
public sealed class OrderController : Controller
{
    private readonly AppDbContext _db;
    private readonly IReceiptPrinterService _printer;

    public OrderController(AppDbContext db, IReceiptPrinterService printer)
    {
        _db = db;
        _printer = printer;
    }

    private int CurrentEmployeeId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        var orders = await _db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return View(orders);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateOrderCommand(OrderChannel.DineIn, null));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderCommand command, bool confirmDuplicateTable = false)
    {
        if (command.Channel == OrderChannel.DineIn && string.IsNullOrWhiteSpace(command.TableNumber))
        {
            ModelState.AddModelError(nameof(command.TableNumber), "內用訂單請填寫桌號");
        }

        if (!ModelState.IsValid)
        {
            return View(command);
        }

        // 同桌可能因併桌而合理地開第二張單，僅提示不強制擋下，需使用者主動勾選確認
        if (command.Channel == OrderChannel.DineIn && !confirmDuplicateTable)
        {
            var existingOpenOrder = await _db.Orders
                .Where(o => o.Status == OrderStatus.Open && o.TableNumber == command.TableNumber)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
            if (existingOpenOrder is not null)
            {
                ViewBag.DuplicateTableOrder = existingOpenOrder;
                return View(command);
            }
        }

        var order = new Order
        {
            Channel = command.Channel,
            TableNumber = command.Channel == OrderChannel.DineIn ? command.TableNumber : null,
            Status = OrderStatus.Open,
            TotalAmount = 0,
            DailyNumber = await GetNextDailyNumberAsync(),
            CreatedByEmployeeId = CurrentEmployeeId
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    /// <summary>以台灣時區（而非容器系統時區）判斷「今天」的邊界，計算當日下一個流水號。</summary>
    private async Task<int> GetNextDailyNumberAsync()
    {
        var todayStartUtc = TaipeiTime.StartOfDayUtc(TaipeiTime.Today);
        var todayEndUtc = todayStartUtc.AddDays(1);

        var maxDailyNumber = await _db.Orders
            .Where(o => o.CreatedAt >= todayStartUtc && o.CreatedAt < todayEndUtc)
            .Select(o => (int?)o.DailyNumber)
            .MaxAsync();

        return (maxDailyNumber ?? 0) + 1;
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .Include(o => o.CreatedByEmployee)
            .Include(o => o.VoidedByEmployee)
            .SingleOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        var activeMenuItems = await _db.MenuItems
            .Where(m => m.IsActive)
            .OrderBy(m => m.Category).ThenBy(m => m.Name)
            .ToListAsync();
        var activeAddOns = await _db.AddOns
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return View(new OrderDetailsViewModel { Order = order, ActiveMenuItems = activeMenuItems, ActiveAddOns = activeAddOns });
    }

    /// <summary>包含加料金額的訂單總額重算，項目集合需已 Include AddOns。</summary>
    private static decimal ComputeTotal(Order order) =>
        order.Items.Sum(i => i.UnitPrice * i.Quantity + i.AddOns.Sum(a => a.UnitPrice * a.Quantity));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(int orderId, AddOrderItemCommand command)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .SingleOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join("；", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        if (order.Status != OrderStatus.Open)
        {
            TempData["Error"] = "訂單已鎖定，無法加點。";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        var menuItem = await _db.MenuItems.SingleOrDefaultAsync(m => m.Id == command.MenuItemId && m.IsActive);
        if (menuItem is null)
        {
            TempData["Error"] = "品項不存在或已下架。";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        var isCustomized = menuItem.SupportsAddOns || menuItem.SupportsSpiceLevel;
        // 客製化品項恆為 1（後端強制，不信任前端），避免多份是否都套用同一客製化的計價歧義
        var newItem = new OrderItem
        {
            MenuItemId = menuItem.Id,
            UnitPrice = menuItem.Price,
            Quantity = isCustomized ? 1 : command.Quantity,
            Note = command.Note,
            SpiceLevel = menuItem.SupportsSpiceLevel ? (command.SpiceLevel ?? Models.Enums.SpiceLevel.Mild) : null
        };

        if (menuItem.SupportsAddOns && command.AddOns is { Count: > 0 })
        {
            var addOnIds = command.AddOns.Select(a => a.AddOnId).Distinct().ToList();
            var activeAddOns = await _db.AddOns.Where(a => addOnIds.Contains(a.Id) && a.IsActive).ToListAsync();
            if (activeAddOns.Count != addOnIds.Count)
            {
                TempData["Error"] = "部分加料已下架，請重新選擇。";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }

            foreach (var selection in command.AddOns)
            {
                var addOn = activeAddOns.Single(a => a.Id == selection.AddOnId);
                newItem.AddOns.Add(new OrderItemAddOn
                {
                    AddOnId = addOn.Id,
                    AddOnName = addOn.Name,
                    UnitPrice = addOn.Price,
                    Quantity = selection.Quantity
                });
            }
        }

        order.Items.Add(newItem);
        // 每次加點皮重新加總，確保金額與品項清單一致（含加料）
        order.TotalAmount = ComputeTotal(order);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"已加點 {menuItem.Name} x{newItem.Quantity}。";

        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPrint(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .SingleOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return NotFound();
        }

        // 確認出單僅限未結帳訂單；結帳時已由 Checkout 補印，作廢訂單則不應再送廚房
        if (order.Status != OrderStatus.Open)
        {
            TempData["Error"] = "訂單已鎖定，無法出單。";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        var printed = await TryPrintPendingItemsAsync(order);
        if (printed is null)
        {
            TempData["Error"] = "目前沒有尚未出單的品項。";
        }
        else if (printed == true)
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "已出單。";
        }
        else
        {
            TempData["Warning"] = "⬆ 出單失敗，請確認印表機後重新按「確認出單」。";
        }

        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    /// <summary>將尚未出單的品項批次列印（首次列為全單，之後列為加點單）；true=列印成功並已標記，false=列印失敗，null=無待出單品項。不負責儲存，調用端需自行 SaveChangesAsync。</summary>
    private async Task<bool?> TryPrintPendingItemsAsync(Order order)
    {
        var pendingItems = order.Items.Where(i => !i.IsPrinted).ToList();
        if (pendingItems.Count == 0)
        {
            return null;
        }

        var isFirstBatch = pendingItems.Count == order.Items.Count;
        var printed = isFirstBatch
            ? await _printer.PrintNewOrderAsync(order)
            : await _printer.PrintAddedItemsAsync(order, pendingItems);

        if (printed)
        {
            foreach (var item in pendingItems)
            {
                item.IsPrinted = true;
            }
        }

        return printed;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelPendingItems(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .SingleOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Open)
        {
            TempData["Error"] = "訂單已鎖定，無法取消。";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        var pendingItems = order.Items.Where(i => !i.IsPrinted).ToList();
        foreach (var item in pendingItems)
        {
            order.Items.Remove(item);
            _db.OrderItems.Remove(item);
        }
        order.TotalAmount = ComputeTotal(order);

        // 訂單從未出過單，代表廚房從未收到通知，直接連同訂單一併作廢，不需使用者輸入理由
        if (order.Items.Count == 0)
        {
            order.Status = OrderStatus.Voided;
            order.VoidReason = "未確認出單，使用者取消";
            order.VoidedByEmployeeId = CurrentEmployeeId;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "已取消。";
        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(int orderId, int orderItemId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .SingleOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Open)
        {
            TempData["Error"] = "訂單已鎖定，無法移除品項。";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        var item = order.Items.SingleOrDefault(i => i.Id == orderItemId);
        if (item is not null)
        {
            order.Items.Remove(item);
            _db.OrderItems.Remove(item);
            order.TotalAmount = ComputeTotal(order);
            await _db.SaveChangesAsync();
            TempData["Success"] = "已移除品項。";
        }

        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .SingleOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Open || order.Items.Count == 0)
        {
            TempData["Error"] = "訂單狀態不允許結帳，或尚未加入任何品項。";
            return RedirectToAction(nameof(Details), new { id });
        }

        // 安全網：若結帳前仍有忘記確認出單的品項，結帳時一併補列印，避免廚房漏單
        var printResult = await TryPrintPendingItemsAsync(order);

        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "結帳完成。";

        if (printResult == false)
        {
            TempData["Warning"] = "⬆ 結帳前仍有品項未出單且列印失敗，請確認印表機並使用「補印」。";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(int id, VoidOrderCommand command)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .SingleOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Open && order.Status != OrderStatus.Completed)
        {
            TempData["Error"] = "此訂單狀態不可作廢。";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join("；", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id });
        }

        // 作廢需留下操作者與原因，供稽核追蹤
        order.Status = OrderStatus.Voided;
        order.VoidReason = command.Reason;
        order.VoidedByEmployeeId = CurrentEmployeeId;
        await _db.SaveChangesAsync();
        TempData["Success"] = "訂單已作廢。";

        // 尚未有任何品項送過廚房，無需列印作廢通知
        var hasPrintedItems = order.Items.Any(i => i.IsPrinted);
        if (hasPrintedItems && !await _printer.PrintVoidNoticeAsync(order))
        {
            TempData["Warning"] = "⬆ 作廢通知列印失敗，請確認印表機並手動告知廚房。";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reprint(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items)
                .ThenInclude(i => i.AddOns)
            .SingleOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        // 已作廢訂單若再印出「全單」，廚房可能誤以為要製作，一律不允許補印
        if (order.Status == OrderStatus.Voided)
        {
            TempData["Error"] = "訂單已作廢，無法補印。";
            return RedirectToAction(nameof(Details), new { id });
        }

        var printed = await _printer.PrintNewOrderAsync(order);
        TempData[printed ? "Success" : "Warning"] = printed ? "已重新列印全單。" : "⬆ 補印失敗，請確認印表機。";
        return RedirectToAction(nameof(Details), new { id });
    }
}
