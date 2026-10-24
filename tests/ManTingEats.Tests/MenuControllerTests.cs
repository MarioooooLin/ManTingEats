using ManTingEats.Controllers;
using ManTingEats.Models;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManTingEats.Tests;

/// <summary>v17 店長自行管理菜單分類：新增、改名、排序、停用規則，以及品項與分類的連動。</summary>
public sealed class MenuControllerTests : IDisposable
{
    // 沿用 OrderController 的測試環境：已有分類「吃」（Id 1）與其下的兩個上架、一個下架品項
    private readonly OrderControllerTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private MenuController CreateController()
    {
        var httpContext = new DefaultHttpContext();
        return new MenuController(_host.CreateDbContext())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new OrderControllerTestHost.NullTempDataProvider())
        };
    }

    private int SeedCategory(string name, int sortOrder, bool isActive = true)
    {
        using var db = _host.CreateDbContext();
        var category = new MenuCategory { Name = name, SortOrder = sortOrder, IsActive = isActive };
        db.MenuCategories.Add(category);
        db.SaveChanges();
        return category.Id;
    }

    private int SeedItem(string name, int categoryId, bool isActive)
    {
        using var db = _host.CreateDbContext();
        var item = new MenuItem { Name = name, Price = 50m, CategoryId = categoryId, IsActive = isActive };
        db.MenuItems.Add(item);
        db.SaveChanges();
        return item.Id;
    }

    private List<string> CategoryNamesInOrder()
    {
        using var db = _host.CreateDbContext();
        return db.MenuCategories.OrderBy(c => c.SortOrder).ThenBy(c => c.Id).Select(c => c.Name).ToList();
    }

    private MenuCategory LoadCategory(int id)
    {
        using var db = _host.CreateDbContext();
        return db.MenuCategories.AsNoTracking().Single(c => c.Id == id);
    }

    private MenuItem LoadItem(int id)
    {
        using var db = _host.CreateDbContext();
        return db.MenuItems.AsNoTracking().Single(m => m.Id == id);
    }

    [Fact]
    public async Task CreateCategory_TrimsNameAndAppendsToEnd()
    {
        SeedCategory("喝", sortOrder: 5);

        var result = await CreateController().CreateCategory(new CreateMenuCategoryCommand("  湯品  "));

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(["吃", "喝", "湯品"], CategoryNamesInOrder());
    }

    [Fact]
    public async Task CreateCategory_DuplicateOfInactiveCategory_ReturnsError()
    {
        SeedCategory("季節限定", sortOrder: 1, isActive: false);
        var controller = CreateController();

        var result = await controller.CreateCategory(new CreateMenuCategoryCommand("季節限定"));

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(2, CategoryNamesInOrder().Count);
    }

    [Fact]
    public async Task EditCategory_Rename_Saves()
    {
        var result = await CreateController().EditCategory(new UpdateMenuCategoryCommand(OrderControllerTestHost.CategoryId, "主食"));

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("主食", LoadCategory(OrderControllerTestHost.CategoryId).Name);
    }

    [Fact]
    public async Task EditCategory_DuplicateName_ReturnsError()
    {
        var drinkId = SeedCategory("喝", sortOrder: 1);
        var controller = CreateController();

        var result = await controller.EditCategory(new UpdateMenuCategoryCommand(drinkId, "吃"));

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal("喝", LoadCategory(drinkId).Name);
    }

    [Fact]
    public async Task ToggleCategoryActive_WithActiveItems_IsRejected()
    {
        // 停用會讓上架品項從點餐頁消失，必須先移走或下架（使用者決議擋下）
        var controller = CreateController();

        await controller.ToggleCategoryActive(OrderControllerTestHost.CategoryId);

        Assert.True(LoadCategory(OrderControllerTestHost.CategoryId).IsActive);
        Assert.Contains("2 個上架品項", controller.TempData["Error"] as string);
    }

    [Fact]
    public async Task ToggleCategoryActive_OnlyInactiveItems_DisablesThenEnables()
    {
        var seasonalId = SeedCategory("季節限定", sortOrder: 1);
        SeedItem("冬瓜茶", seasonalId, isActive: false);

        await CreateController().ToggleCategoryActive(seasonalId);
        Assert.False(LoadCategory(seasonalId).IsActive);

        await CreateController().ToggleCategoryActive(seasonalId);
        Assert.True(LoadCategory(seasonalId).IsActive);
    }

    [Fact]
    public async Task MoveCategory_Up_SwapsWithPrevious()
    {
        SeedCategory("喝", sortOrder: 1);
        var otherId = SeedCategory("其他", sortOrder: 2);

        await CreateController().MoveCategory(otherId, direction: -1);

        Assert.Equal(["吃", "其他", "喝"], CategoryNamesInOrder());
    }

    [Fact]
    public async Task MoveCategory_FirstUpOrLastDown_KeepsOrder()
    {
        var drinkId = SeedCategory("喝", sortOrder: 1);

        await CreateController().MoveCategory(OrderControllerTestHost.CategoryId, direction: -1);
        await CreateController().MoveCategory(drinkId, direction: 1);

        Assert.Equal(["吃", "喝"], CategoryNamesInOrder());
    }

    [Fact]
    public async Task MoveCategory_SkipsInactiveCategoriesAndKeepsTheirPosition()
    {
        // 停用分類收在列表最下方、不在點餐頁出現，上移應直接越過它與上一個啟用分類交換
        SeedCategory("季節限定", sortOrder: 1, isActive: false);
        var drinkId = SeedCategory("喝", sortOrder: 2);

        await CreateController().MoveCategory(drinkId, direction: -1);

        Assert.Equal(["喝", "季節限定", "吃"], CategoryNamesInOrder());
    }

    [Fact]
    public async Task MoveCategory_InactiveCategory_KeepsOrder()
    {
        var seasonalId = SeedCategory("季節限定", sortOrder: 1, isActive: false);

        await CreateController().MoveCategory(seasonalId, direction: -1);

        Assert.Equal(["吃", "季節限定"], CategoryNamesInOrder());
    }

    [Fact]
    public async Task MoveCategory_DuplicateSortOrders_StillMovesAndRenumbers()
    {
        // 排序值重複時只交換數值會無效，因此交換後依新順序重新編號
        var drinkId = SeedCategory("喝", sortOrder: 0);

        await CreateController().MoveCategory(drinkId, direction: -1);

        Assert.Equal(["喝", "吃"], CategoryNamesInOrder());
        using var db = _host.CreateDbContext();
        Assert.Equal([0, 1], db.MenuCategories.OrderBy(c => c.SortOrder).Select(c => c.SortOrder).ToList());
    }

    [Fact]
    public async Task CreateItem_InactiveCategory_ReturnsError()
    {
        var seasonalId = SeedCategory("季節限定", sortOrder: 1, isActive: false);
        var controller = CreateController();

        var result = await controller.Create(new CreateMenuItemCommand("冬瓜茶", seasonalId, 30m));

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        using var db = _host.CreateDbContext();
        Assert.DoesNotContain(db.MenuItems, m => m.Name == "冬瓜茶");
    }

    [Fact]
    public async Task CreateItem_ActiveCategory_Saves()
    {
        var drinkId = SeedCategory("喝", sortOrder: 1);

        var result = await CreateController().Create(new CreateMenuItemCommand("冬瓜茶", drinkId, 30m));

        Assert.IsType<RedirectToActionResult>(result);
        using var db = _host.CreateDbContext();
        Assert.Equal(drinkId, db.MenuItems.Single(m => m.Name == "冬瓜茶").CategoryId);
    }

    [Fact]
    public async Task EditItem_KeepsCurrentInactiveCategory_Saves()
    {
        // 停用分類中的下架品項改價格時，不應被迫換分類
        var seasonalId = SeedCategory("季節限定", sortOrder: 1);
        var itemId = SeedItem("冬瓜茶", seasonalId, isActive: false);
        await CreateController().ToggleCategoryActive(seasonalId);

        var result = await CreateController().Edit(new UpdateMenuItemCommand(itemId, "冬瓜茶", seasonalId, 35m));

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(35m, LoadItem(itemId).Price);
    }

    [Fact]
    public async Task EditItem_MoveToOtherInactiveCategory_ReturnsError()
    {
        var seasonalId = SeedCategory("季節限定", sortOrder: 1, isActive: false);
        var controller = CreateController();

        var result = await controller.Edit(new UpdateMenuItemCommand(OrderControllerTestHost.SimpleItemId, "酸梅湯", seasonalId, 100m));

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(OrderControllerTestHost.CategoryId, LoadItem(OrderControllerTestHost.SimpleItemId).CategoryId);
    }

    [Fact]
    public async Task ToggleItemActive_CategoryInactive_IsRejected()
    {
        // 否則可繞過「停用前須先下架」，讓上架品項落在停用分類中
        var seasonalId = SeedCategory("季節限定", sortOrder: 1);
        var itemId = SeedItem("冬瓜茶", seasonalId, isActive: false);
        await CreateController().ToggleCategoryActive(seasonalId);
        var controller = CreateController();

        await controller.ToggleActive(itemId);

        Assert.False(LoadItem(itemId).IsActive);
        Assert.Contains("已停用", controller.TempData["Error"] as string);
    }

    [Fact]
    public async Task Index_CategoriesInSortOrderWithItemCounts()
    {
        var drinkId = SeedCategory("喝", sortOrder: -1);
        SeedItem("冬瓜茶", drinkId, isActive: false);

        var result = await CreateController().Index();

        var model = Assert.IsType<MenuIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(["喝", "吃"], model.Categories.Select(c => c.Category.Name));
        var food = model.Categories.Single(c => c.Category.Id == OrderControllerTestHost.CategoryId);
        Assert.Equal((2, 3), (food.ActiveItemCount, food.ItemCount));
        Assert.Equal("冬瓜茶", model.Items.First().Name);
    }

    [Fact]
    public async Task OrderDetails_MenuItemsFollowCategorySortOrder()
    {
        var drinkId = SeedCategory("喝", sortOrder: -1);
        SeedItem("冬瓜茶", drinkId, isActive: true);
        var orderId = _host.SeedOrder();

        var result = await _host.CreateController().Details(orderId);

        var model = Assert.IsType<OrderDetailsViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(["冬瓜茶", "爆辣冷麵", "酸梅湯"], model.ActiveMenuItems.Select(m => m.Name));
    }
}
