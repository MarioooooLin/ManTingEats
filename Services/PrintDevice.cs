namespace ManTingEats.Services;

/// <summary>
/// 判斷目前裝置能否出單（v14，2026-10-13 使用者決議）：印表機只接在店內 iPad，手機點餐不開 PassPRNT，改提示到 iPad 出單。
/// </summary>
public static class PrintDevice
{
    /// <summary>
    /// 依 User-Agent 判斷是否為手機：iPhone／iPod，或含「Mobile」的 Android（Android 平板不含 Mobile）。
    /// iPadOS 13 起 Safari 預設以 Mac 桌機身分送出 User-Agent，因此 iPad 不會被判為手機；
    /// 代價是 Mac 電腦也視為可出單裝置，店內只用 iPad 與手機，可以接受。
    /// </summary>
    public static bool IsPhone(string? userAgent) =>
        !string.IsNullOrEmpty(userAgent)
        && (userAgent.Contains("iPhone", StringComparison.Ordinal)
            || userAgent.Contains("iPod", StringComparison.Ordinal)
            || (userAgent.Contains("Android", StringComparison.Ordinal) && userAgent.Contains("Mobile", StringComparison.Ordinal)));
}
