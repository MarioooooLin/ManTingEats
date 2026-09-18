using ManTingEats.Data;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManTingEats.Controllers;

[Authorize]
public sealed class MenuController : Controller
{
    private readonly AppDbContext _db;

    public MenuController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _db.MenuItems
            .OrderBy(m => m.Category)
            .ThenBy(m => m.Name)
            .ToListAsync();
        return View(items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateMenuItemCommand(string.Empty, string.Empty, 0));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMenuItemCommand command)
    {
        if (!ModelState.IsValid)
        {
            return View(command);
        }

        _db.MenuItems.Add(new MenuItem
        {
            Name = command.Name,
            Category = command.Category,
            Price = command.Price,
            IsActive = true
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "已新增品項。";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        return View(new UpdateMenuItemCommand(item.Id, item.Name, item.Category, item.Price));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateMenuItemCommand command)
    {
        if (!ModelState.IsValid)
        {
            return View(command);
        }

        var item = await _db.MenuItems.FindAsync(command.Id);
        if (item is null)
        {
            return NotFound();
        }

        item.Name = command.Name;
        item.Category = command.Category;
        item.Price = command.Price;
        await _db.SaveChangesAsync();
        TempData["Success"] = "已更新品項。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.IsActive = !item.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = item.IsActive ? "已上架。" : "已下架。";
        return RedirectToAction(nameof(Index));
    }
}
