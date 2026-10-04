using ManTingEats.Models.Entities;
using ManTingEats.Services;
using Xunit;

namespace ManTingEats.Tests;

public class OrderTicketTests
{
    private static Order CreateOrder(params (int Id, bool IsPrinted)[] items)
    {
        var order = new Order();
        foreach (var (id, isPrinted) in items)
        {
            order.Items.Add(new OrderItem { Id = id, Quantity = 1, IsPrinted = isPrinted });
        }
        return order;
    }

    [Fact]
    public void IsFirstBatch_NoItemPrinted_ReturnsTrue()
    {
        var order = CreateOrder((1, false), (2, false));

        Assert.True(OrderTicket.IsFirstBatch(order));
    }

    [Fact]
    public void IsFirstBatch_SomeItemPrinted_ReturnsFalse()
    {
        var order = CreateOrder((1, true), (2, false));

        Assert.False(OrderTicket.IsFirstBatch(order));
        Assert.Equal([2], OrderTicket.PendingItems(order).Select(i => i.Id));
    }

    [Fact]
    public void MarkPrinted_OnlyMarksItemsSentToPrinter()
    {
        // 送印時只有 1、2；列印期間加點了 3，回呼後 3 必須維持待出單
        var order = CreateOrder((1, false), (2, false), (3, false));

        var marked = OrderTicket.MarkPrinted(order, [1, 2]);

        Assert.Equal(2, marked);
        Assert.Equal([3], OrderTicket.PendingItems(order).Select(i => i.Id));
    }

    [Fact]
    public void MarkPrinted_IgnoresRemovedOrAlreadyPrintedItems()
    {
        // 9 在送印後被移除；1 已經是已出單（例如重複送出回呼）
        var order = CreateOrder((1, true), (2, false));

        var marked = OrderTicket.MarkPrinted(order, [1, 2, 9]);

        Assert.Equal(1, marked);
        Assert.All(order.Items, i => Assert.True(i.IsPrinted));
    }

    [Fact]
    public void ItemIds_RoundTripThroughCallbackUrl()
    {
        var order = CreateOrder((5, false), (12, false));

        var formatted = OrderTicket.FormatItemIds(order.Items);

        Assert.Equal("5,12", formatted);
        Assert.Equal([5, 12], OrderTicket.ParseItemIds(formatted));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc,,")]
    public void ParseItemIds_InvalidInput_ReturnsEmpty(string? value)
    {
        Assert.Empty(OrderTicket.ParseItemIds(value));
    }

    [Fact]
    public void ParseItemIds_SkipsInvalidSegments()
    {
        Assert.Equal([3, 7], OrderTicket.ParseItemIds("3, x ,7"));
    }
}
