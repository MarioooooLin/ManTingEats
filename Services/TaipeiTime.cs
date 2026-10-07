namespace ManTingEats.Services;

/// <summary>店家營運日以台灣時區為準；資料庫時間戳記一律存 UTC，查詢「某天」時需先換算成 UTC 邊界，不可依賴容器系統時區。</summary>
public static class TaipeiTime
{
    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    /// <summary>
    /// 營業日切換時間（v9）：營業時間 17:00–02:00 會跨過午夜，若以 00:00 切日，同一晚的單號會歸零重來、營收被拆成兩天；
    /// 改以 06:00 切換，打烊後留足收尾緩衝，且早於每日 08:00 的自動備份。
    /// </summary>
    public static readonly TimeSpan BusinessDayCutoff = TimeSpan.FromHours(6);

    /// <summary>台灣時區的現在時間。</summary>
    public static DateTime Now => DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone), DateTimeKind.Unspecified);

    /// <summary>台灣時區的今天日期（日曆日，訂位、支出等依實際日期的功能使用）。</summary>
    public static DateTime Today => Now.Date;

    /// <summary>目前所屬的營業日（單號、營收報表使用）；例如 01:30 仍屬前一天的營業日。</summary>
    public static DateTime BusinessToday => BusinessDateOf(Now);

    /// <summary>將台灣時區的日期（當天 00:00）換算成對應的 UTC 時間點。</summary>
    public static DateTime StartOfDayUtc(DateTime taipeiDate) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(taipeiDate.Date, DateTimeKind.Unspecified), TimeZone);

    /// <summary>台灣時間所屬的營業日：切換時間之前算前一天。獨立成純函式，方便測試邊界。</summary>
    public static DateTime BusinessDateOf(DateTime taipeiTime) => (taipeiTime - BusinessDayCutoff).Date;

    /// <summary>營業日的開始時間點（當天切換時間，台灣時區）換算成 UTC。</summary>
    public static DateTime BusinessDayStartUtc(DateTime businessDate) =>
        StartOfDayUtc(businessDate).Add(BusinessDayCutoff);
}
