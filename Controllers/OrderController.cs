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
    public async Task<IActionResult> Create(CreateOrderCommand command)
    {
        if (command.Channel == OrderChannel.DineIn && string.IsNullOrWhiteSpace(command.TableNumber))
        {
            ModelState.AddModelError(nameof(command.TableNumber), "內用訂單請填寫桌號");
        }

        if (!ModelState.IsValid)
        {
            return View(command);
        }

        var order = new Order
        {
            Channel = command.Channel,
            TableNumber = command.Channel == OrderChannel.DineIn ? command.TableNumber : null,
            Status = OrderStatus.Open,
            TotalAmount = 0,
            CreatedByEmployeeId = CurrentEmployeeId
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
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

        return View(new OrderDetailsViewModel { Order = order, ActiveMenuItems = activeMenuItems });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(int orderId, AddOrderItemCommand command)
    {
        var order = await _db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return NotFound();
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

        var newItem = new OrderItem
        {
            MenuItemId = menuItem.Id,
            UnitPrice = menuItem.Price,
            Quantity = command.Quantity,
            Note = command.Note
        };
        order.Items.Add(newItem);
        // 每次加點皮重新加總，確保金額與品項清單一致
        order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"已加點 {menuItem.Name} x{command.Quantity}。";

        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPrint(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
            .SingleOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            return NotFound();
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
    public async Task<IActionResult> RemoveItem(int orderId, int orderItemId)
    {
        var order = await _db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == orderId);
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
            order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);
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
            .SingleOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Completed)
        {
            TempData["Error"] = "僅已結帳訂單可作廢。";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            TempData["Error"] = "請輸入作廢原因。";
            return RedirectToAction(nameof(Details), new { id });
        }

        // 作廢需留下操作者與原因，供稽核追蹤
        order.Status = OrderStatus.Voided;
        order.VoidReason = command.Reason;
        order.VoidedByEmployeeId = CurrentEmployeeId;
        await _db.SaveChangesAsync();
        TempData["Success"] = "訂單已作廢。";

        if (!await _printer.PrintVoidNoticeAsync(order))
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
            .SingleOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        var printed = await _printer.PrintNewOrderAsync(order);
        TempData[printed ? "Success" : "Warning"] = printed ? "已重新列印全單。" : "⬆ 補印失敗，請確認印表機。";
        return RedirectToAction(nameof(Details), new { id });
    }
}
