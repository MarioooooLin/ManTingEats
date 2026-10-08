using ManTingEats.Controllers;
using ManTingEats.Models;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static ManTingEats.Tests.OrderControllerTestHost;

namespace ManTingEats.Tests;

public sealed class ReportControllerTests : IDisposable
{
    // 沿用 OrderController 的測試環境，取其中已建立的菜單、加料與員工資料
    private readonly OrderControllerTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private static OrderItem WithEggs(int eggs, int servings = 1) => new()
    {
        MenuItemId = CustomItemId,
        UnitPrice = 120m,
        Quantity = servings,
        AddOns = { new OrderItemAddOn { AddOnId = EggAddOnId, AddOnName = "加蛋", UnitPrice = 10m, Quantity = eggs } }
    };

    private void CompleteAll()
    {
        using var db = _host.CreateDbContext();
        foreach (var order in db.Orders)
        {
            order.Status = OrderStatus.Completed;
            order.CompletedAt = DateTime.UtcNow;
        }
        db.SaveChanges();
    }

    private async Task<ReportViewModel> LoadReport()
    {
        var result = await new ReportController(_host.CreateDbContext()).Index(null, null);
        return Assert.IsType<ReportViewModel>(Assert.IsType<ViewResult>(result).Model);
    }

    [Fact]
    public async Task Index_SplitsItemAndAddOnRankings_AndTheySumToRevenue()
    {
        _host.SeedOrder(OrderStatus.Open, WithEggs(2), Item(quantity: 3));
        _host.SeedOrder(OrderStatus.Open, WithEggs(1));
        CompleteAll();

        var report = await LoadReport();

        // 品項金額只算品項本身：冷麵 2 碗 × $120，不含加蛋
        var noodle = Assert.Single(report.ItemRanking, i => i.MenuItemName == "爆辣冷麵");
        Assert.Equal(2, noodle.Quantity);
        Assert.Equal(240m, noodle.Amount);

        var egg = Assert.Single(report.AddOnRanking);
        Assert.Equal("加蛋", egg.AddOnName);
        Assert.Equal(3, egg.Quantity);
        Assert.Equal(30m, egg.Amount);

        Assert.Equal(report.TotalRevenue, report.ItemRanking.Sum(i => i.Amount) + report.AddOnRanking.Sum(a => a.Amount));
    }

    [Fact]
    public async Task Index_MultipleServings_AddOnQuantityTimesServings()
    {
        // v10：冷麵 3 份、每份加蛋 2 顆 → 加蛋共 6 顆
        _host.SeedOrder(OrderStatus.Open, WithEggs(2, servings: 3));
        CompleteAll();

        var report = await LoadReport();

        var noodle = Assert.Single(report.ItemRanking);
        Assert.Equal(3, noodle.Quantity);
        Assert.Equal(360m, noodle.Amount);

        var egg = Assert.Single(report.AddOnRanking);
        Assert.Equal(6, egg.Quantity);
        Assert.Equal(60m, egg.Amount);

        Assert.Equal(420m, report.TotalRevenue);
        Assert.Equal(report.TotalRevenue, report.ItemRanking.Sum(i => i.Amount) + report.AddOnRanking.Sum(a => a.Amount));
    }

    [Fact]
    public async Task Index_ExcludesAddOnsOfUncompletedOrders()
    {
        _host.SeedOrder(OrderStatus.Open, WithEggs(1));
        _host.SeedOrder(OrderStatus.Voided, WithEggs(1));

        var report = await LoadReport();

        Assert.Empty(report.AddOnRanking);
    }
}
