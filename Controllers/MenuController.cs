using ManTingEats.Data;
using ManTingEats.Models;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ManTingEats.Controllers;

// 店長限定（v7）：員工只能使用訂單與訂位
[Authorize(Roles = AppRoles.Manager)]
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
            .Include(m => m.Category)
            .OrderBy(m => m.Category.SortOrder).ThenBy(m => m.CategoryId)
            .ThenBy(m => m.Name)
            .ToListAsync();
        var addOns = await _db.AddOns
            .OrderBy(a => a.Name)
            .ToListAsync();
        var categories = await _db.MenuCategories
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(c => new MenuCategoryRow(c, c.MenuItems.Count(m => m.IsActive), c.MenuItems.Count))
            .ToListAsync();
        return View(new MenuIndexViewModel { Items = items, AddOns = addOns, Categories = categories });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await SetCategoryOptionsAsync(currentCategoryId: null);
        var defaultCategoryId = await ActiveCategories().Select(c => c.Id).FirstOrDefaultAsync();
        return View(new CreateMenuItemCommand(string.Empty, defaultCategoryId, 0));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMenuItemCommand command)
    {
        command = command with { Name = command.Name?.Trim() ?? string.Empty };
        await ValidateUniqueNameAsync(command.Name, excludeId: null);
        await ValidateCategoryAsync(command.CategoryId, currentCategoryId: null);
        if (!ModelState.IsValid)
        {
            await SetCategoryOptionsAsync(currentCategoryId: null);
            return View(command);
        }

        _db.MenuItems.Add(new MenuItem
        {
            Name = command.Name,
            CategoryId = command.CategoryId,
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

        await SetCategoryOptionsAsync(item.CategoryId);
        return View(new UpdateMenuItemCommand(item.Id, item.Name, item.CategoryId, item.Price, item.SupportsAddOns, item.SupportsSpiceLevel));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateMenuItemCommand command)
    {
        command = command with { Name = command.Name?.Trim() ?? string.Empty };
        var item = await _db.MenuItems.FindAsync(command.Id);
        if (item is null)
        {
            return NotFound();
        }

        await ValidateUniqueNameAsync(command.Name, excludeId: command.Id);
        await ValidateCategoryAsync(command.CategoryId, item.CategoryId);
        if (!ModelState.IsValid)
        {
            await SetCategoryOptionsAsync(item.CategoryId);
            return View(command);
        }

        item.Name = command.Name;
        item.CategoryId = command.CategoryId;
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

    private IQueryable<MenuCategory> ActiveCategories() =>
        _db.MenuCategories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ThenBy(c => c.Id);

    /// <summary>
    /// 品項只能放在啟用中的分類（v17），維持「上架品項的分類一定啟用」。
    /// 編輯時保留原本已停用的分類可以通過（品項必為下架），否則改其他欄位存檔時會被迫換分類。
    /// </summary>
    private async Task ValidateCategoryAsync(int categoryId, int? currentCategoryId)
    {
        if (categoryId == currentCategoryId)
        {
            return;
        }

        if (!await _db.MenuCategories.AnyAsync(c => c.Id == categoryId && c.IsActive))
        {
            ModelState.AddModelError(nameof(CreateMenuItemCommand.CategoryId), "分類不存在或已停用，請重新選擇");
        }
    }

    /// <summary>分類下拉選單：啟用中的分類，編輯時另含品項目前的分類（即使已停用）。</summary>
    private async Task SetCategoryOptionsAsync(int? currentCategoryId)
    {
        var categories = await _db.MenuCategories
            .Where(c => c.IsActive || c.Id == currentCategoryId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .ToListAsync();
        ViewData["CategoryOptions"] = categories
            .Select(c => new SelectListItem(c.IsActive ? c.Name : $"{c.Name}（已停用）", c.Id.ToString()))
            .ToList();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var item = await _db.MenuItems.Include(m => m.Category).SingleOrDefaultAsync(m => m.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        if (!item.IsActive && !item.Category.IsActive)
        {
            TempData["Error"] = $"「{item.Name}」的分類「{item.Category.Name}」已停用，請先啟用分類，或編輯品項改到其他分類後再上架。";
            return RedirectToAction(nameof(Index));
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
        return RedirectToAction(nameof(Index), null, "addons");
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
        return RedirectToAction(nameof(Index), null, "addons");
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
        return RedirectToAction(nameof(Index), null, "addons");
    }

    [HttpGet]
    public IActionResult CreateCategory()
    {
        return View(new CreateMenuCategoryCommand(string.Empty));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CreateMenuCategoryCommand command)
    {
        command = command with { Name = command.Name?.Trim() ?? string.Empty };
        await ValidateUniqueCategoryNameAsync(command.Name, excludeId: null);
        if (!ModelState.IsValid)
        {
            return View(command);
        }

        // 新分類排在最後；店長再以上移調整
        var maxSortOrder = await _db.MenuCategories.MaxAsync(c => (int?)c.SortOrder) ?? -1;
        _db.MenuCategories.Add(new MenuCategory { Name = command.Name, SortOrder = maxSortOrder + 1, IsActive = true });
        await _db.SaveChangesAsync();
        TempData["Success"] = "已新增分類。";
        return RedirectToAction(nameof(Index), null, "categories");
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id)
    {
        var category = await _db.MenuCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        return View(new UpdateMenuCategoryCommand(category.Id, category.Name));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(UpdateMenuCategoryCommand command)
    {
        command = command with { Name = command.Name?.Trim() ?? string.Empty };
        await ValidateUniqueCategoryNameAsync(command.Name, excludeId: command.Id);
        if (!ModelState.IsValid)
        {
            return View(command);
        }

        var category = await _db.MenuCategories.FindAsync(command.Id);
        if (category is null)
        {
            return NotFound();
        }

        category.Name = command.Name;
        await _db.SaveChangesAsync();
        TempData["Success"] = "已更新分類。";
        return RedirectToAction(nameof(Index), null, "categories");
    }

    /// <summary>
    /// 分類只能停用不能刪除（v17）。還有上架品項時擋下，避免品項在點餐頁無聲消失；
    /// 已下架的品項可留在停用分類中，日後啟用分類即可恢復。
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCategoryActive(int id)
    {
        var category = await _db.MenuCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        if (category.IsActive)
        {
            var activeItemCount = await _db.MenuItems.CountAsync(m => m.CategoryId == id && m.IsActive);
            if (activeItemCount > 0)
            {
                TempData["Error"] = $"分類「{category.Name}」還有 {activeItemCount} 個上架品項，請先將品項移到其他分類或下架，再停用分類。";
                return RedirectToAction(nameof(Index), null, "categories");
            }
        }

        category.IsActive = !category.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = category.IsActive ? "已啟用分類。" : "已停用分類。";
        return RedirectToAction(nameof(Index), null, "categories");
    }

    /// <summary>
    /// 與上一個／下一個「啟用中」的分類交換位置（direction：-1 上移、1 下移）。
    /// 順序只影響點餐頁（只顯示啟用分類），因此跳過停用分類；停用分類維持原位，重新啟用後回到原本位置。
    /// 交換後依新順序全部重新編號，即使舊資料的排序值重複也能正確移動；一次 SaveChanges 寫入，不會只改到一半。
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveCategory(int id, int direction)
    {
        var categories = await _db.MenuCategories
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .ToListAsync();
        var index = categories.FindIndex(c => c.Id == id);
        if (index < 0)
        {
            return NotFound();
        }

        var step = Math.Sign(direction);
        var target = index + step;
        while (step != 0 && target >= 0 && target < categories.Count && !categories[target].IsActive)
        {
            target += step;
        }

        if (categories[index].IsActive && step != 0 && target >= 0 && target < categories.Count)
        {
            (categories[index], categories[target]) = (categories[target], categories[index]);
            for (var i = 0; i < categories.Count; i++)
            {
                categories[i].SortOrder = i;
            }
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index), null, "categories");
    }

    /// <summary>分類名稱即點餐頁的分頁標籤，重複會分不出來，故不可重複（含已停用分類）。</summary>
    private async Task ValidateUniqueCategoryNameAsync(string name, int? excludeId)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var isDuplicate = await _db.MenuCategories.AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId));
        if (isDuplicate)
        {
            ModelState.AddModelError(nameof(CreateMenuCategoryCommand.Name), "已有相同名稱的分類（含已停用），請改用其他名稱");
        }
    }
}
