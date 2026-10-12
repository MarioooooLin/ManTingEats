using ManTingEats.Services;
using Xunit;

namespace ManTingEats.Tests;

public sealed class PrintDeviceTests
{
    [Theory]
    // iPhone Safari
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1", true)]
    // Android 手機 Chrome
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Mobile Safari/537.36", true)]
    // iPad Safari（iPadOS 13 起以 Mac 桌機身分送出）：可出單
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Safari/605.1.15", false)]
    // Android 平板（不含 Mobile）
    [InlineData("Mozilla/5.0 (Linux; Android 14; SM-X710) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsPhone_ByUserAgent(string? userAgent, bool expected) =>
        Assert.Equal(expected, PrintDevice.IsPhone(userAgent));
}
