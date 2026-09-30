namespace ManTingEats.Services;

/// <summary>店家營運日以台灣時區為準；資料庫時間戳記一律存 UTC，查詢「某天」時需先換算成 UTC 邊界，不可依賴容器系統時區。</summary>
public static class TaipeiTime
{
    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    /// <summary>台灣時區的現在時間。</summary>
    public static DateTime Now => DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone), DateTimeKind.Unspecified);

    /// <summary>台灣時區的今天日期。</summary>
    public static DateTime Today => Now.Date;

    /// <summary>將台灣時區的日期（當天 00:00）換算成對應的 UTC 時間點。</summary>
    public static DateTime StartOfDayUtc(DateTime taipeiDate) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(taipeiDate.Date, DateTimeKind.Unspecified), TimeZone);
}
