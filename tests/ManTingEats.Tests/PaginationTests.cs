using ManTingEats.Models;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ManTingEats.Tests;

public sealed class PaginationTests : IDisposable
{
    private readonly OrderControllerTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private void SeedOrders(int count, OrderStatus status)
    {
        for (var i = 0; i < count; i++)
        {
            _host.SeedOrder(status);
        }
    }

    [Theory]
    [InlineData(1, 1, 20)]
    [InlineData(2, 2, 5)]
    [InlineData(0, 1, 20)]     // 頁碼過小夾回第一頁
    [InlineData(99, 2, 5)]     // 頁碼超過總頁數夾回最後一頁
    public async Task CreateAsync_SplitsIntoPages(int requestedPage, int expectedPage, int expectedCount)
    {
        SeedOrders(25, OrderStatus.Completed);
        using var db = _host.CreateDbContext();

        var result = await PagedList<Order>.CreateAsync(db.Orders.OrderBy(o => o.Id), requestedPage);

        Assert.Equal(expectedPage, result.Page);
        Assert.Equal(expectedCount, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task CreateAsync_NoData_IsSinglePageWithNoItems()
    {
        using var db = _host.CreateDbContext();

        var result = await PagedList<Order>.CreateAsync(db.Orders.OrderBy(o => o.Id), 3);

        Assert.Equal(1, result.Page);
        Assert.Equal(1, result.TotalPages);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task OrderIndex_OpenOrdersStayOnFirstPage()
    {
        // 先開的 2 張未結帳單，之後又結帳了 25 張：若只依時間排序，未結帳單會被擠到第二頁
        SeedOrders(2, OrderStatus.Open);
        SeedOrders(25, OrderStatus.Completed);

        var result = await _host.CreateController().Index(page: 1);

        var orders = Assert.IsType<PagedList<Order>>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(27, orders.TotalCount);
        Assert.All(orders.Items.Take(2), o => Assert.Equal(OrderStatus.Open, o.Status));
    }
}
