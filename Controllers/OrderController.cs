using System.Security.Claims;
using ManTingEats.Data;
using ManTingEats.Models;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManTingEats.Controllers;

[Authorize]
public sealed class OrderController : Controller
{
    private readonly AppDbContext _db;

    public OrderController(AppDbContext db)
    {
        _db = db;
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

        order.Items.Add(new OrderItem
        {
            MenuItemId = menuItem.Id,
            UnitPrice = menuItem.Price,
            Quantity = command.Quantity
        });
        // 每次加點皆重新加總，確保金額與品項清單一致
        order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"已加點 {menuItem.Name} x{command.Quantity}。";
        return RedirectToAction(nameof(Details), new { id = orderId });
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
        var order = await _db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Open || order.Items.Count == 0)
        {
            TempData["Error"] = "訂單狀態不允許結帳，或尚未加入任何品項。";
            return RedirectToAction(nameof(Details), new { id });
        }

        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "結帳完成。";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(int id, VoidOrderCommand command)
    {
        var order = await _db.Orders.SingleOrDefaultAsync(o => o.Id == id);
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
        return RedirectToAction(nameof(Details), new { id });
    }
}
