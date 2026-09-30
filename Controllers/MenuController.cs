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
        var addOns = await _db.AddOns
            .OrderBy(a => a.Name)
            .ToListAsync();
        return View(new MenuIndexViewModel { Items = items, AddOns = addOns });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateMenuItemCommand(string.Empty, MenuCategory.Food, 0));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMenuItemCommand command)
    {
        command = command with { Name = command.Name?.Trim() ?? string.Empty };
        await ValidateUniqueNameAsync(command.Name, excludeId: null);
        if (!ModelState.IsValid)
        {
            return View(command);
        }

        _db.MenuItems.Add(new MenuItem
        {
            Name = command.Name,
            Category = command.Category,
            Price = command.Price,
            IsActive = true,
            SupportsAddOns = command.SupportsAddOns,
            SupportsSpiceLevel = command.SupportsSpiceLevel
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

        return View(new UpdateMenuItemCommand(item.Id, item.Name, item.Category, item.Price, item.SupportsAddOns, item.SupportsSpiceLevel));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateMenuItemCommand command)
    {
        command = command with { Name = command.Name?.Trim() ?? string.Empty };
        await ValidateUniqueNameAsync(command.Name, excludeId: command.Id);
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
        item.SupportsAddOns = command.SupportsAddOns;
        item.SupportsSpiceLevel = command.SupportsSpiceLevel;
        await _db.SaveChangesAsync();
        TempData["Success"] = "已更新品項。";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>營收報表的品項排行以名稱分組，同名品項會被合併計算，故名稱不可重複（含已下架品項）。</summary>
    private async Task ValidateUniqueNameAsync(string name, int? excludeId)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var isDuplicate = await _db.MenuItems.AnyAsync(m => m.Name == name && (excludeId == null || m.Id != excludeId));
        if (isDuplicate)
        {
            ModelState.AddModelError(nameof(CreateMenuItemCommand.Name), "已有相同名稱的品項（含已下架），請改用其他名稱");
        }
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

    [HttpGet]
    public IActionResult CreateAddOn()
    {
        return View(new CreateAddOnCommand(string.Empty, 0));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAddOn(CreateAddOnCommand command)
    {
        if (!ModelState.IsValid)
        {
            return View(command);
        }

        _db.AddOns.Add(new AddOn { Name = command.Name, Price = command.Price, IsActive = true });
        await _db.SaveChangesAsync();
        TempData["Success"] = "已新增加料。";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditAddOn(int id)
    {
        var addOn = await _db.AddOns.FindAsync(id);
        if (addOn is null)
        {
            return NotFound();
        }

        return View(new UpdateAddOnCommand(addOn.Id, addOn.Name, addOn.Price));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAddOn(UpdateAddOnCommand command)
    {
        if (!ModelState.IsValid)
        {
            return View(command);
        }

        var addOn = await _db.AddOns.FindAsync(command.Id);
        if (addOn is null)
        {
            return NotFound();
        }

        addOn.Name = command.Name;
        addOn.Price = command.Price;
        await _db.SaveChangesAsync();
        TempData["Success"] = "已更新加料。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAddOnActive(int id)
    {
        var addOn = await _db.AddOns.FindAsync(id);
        if (addOn is null)
        {
            return NotFound();
        }

        addOn.IsActive = !addOn.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = addOn.IsActive ? "已上架。" : "已下架。";
        return RedirectToAction(nameof(Index));
    }
}
