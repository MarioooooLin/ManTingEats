using ManTingEats.Services;
using Xunit;

namespace ManTingEats.Tests;

public class CashPaymentTests
{
    [Fact]
    public void Validate_ExactOrMore_IsValid()
    {
        Assert.Null(CashPayment.Validate(285m, 285m));
        Assert.Null(CashPayment.Validate(1000m, 285m));
    }

    [Fact]
    public void Validate_Missing_ReturnsError()
    {
        Assert.NotNull(CashPayment.Validate(null, 285m));
    }

    [Fact]
    public void Validate_NotEnough_ShowsShortfall()
    {
        Assert.Equal("收到金額不足，還差 $85。", CashPayment.Validate(200m, 285m));
    }

    [Theory]
    [InlineData(300.5)]
    [InlineData(100001)]
    public void Validate_NonIntegerOrTooLarge_ReturnsError(double received)
    {
        Assert.NotNull(CashPayment.Validate((decimal)received, 285m));
    }
}
