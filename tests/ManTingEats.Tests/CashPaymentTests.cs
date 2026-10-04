using ManTingEats.Services;
using Xunit;

namespace ManTingEats.Tests;

public class CashPaymentTests
{
    [Theory]
    [InlineData(285, new[] { 300, 500, 1000 })]
    [InlineData(650, new[] { 700, 1000 })]
    [InlineData(300, new[] { 400, 500, 1000 })]   // 剛好整百時不列 $300，已有「收剛好」
    [InlineData(450, new[] { 500, 1000 })]         // 下一個整百即 $500，不重複列出
    [InlineData(1250, new[] { 1300 })]
    public void QuickAmounts_ReturnsBillsAboveTotal(int total, int[] expected)
    {
        var amounts = CashPayment.QuickAmounts(total);

        Assert.Equal(expected.Select(e => (decimal)e), amounts);
    }

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
