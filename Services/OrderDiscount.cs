using ManTingEats.Models.Enums;
using ManTingEats.Models.Extensions;

namespace ManTingEats.Services;

/// <summary>
/// 結帳折扣規則（v8）。畫面上的「改收金額／打折／抹零」都換算成「折扣後應收金額」送到後端，
/// 後端只需檢查這個金額與原因；打折與抹零的換算在前端，規則與此處的 RoundOff／ApplyRate 一致。
/// </summary>
public static class OrderDiscount
{
    public const int MaxNoteLength = 100;

    /// <summary>抹零：去掉個位數（$487 → $480）。</summary>
    public static decimal RoundOff(decimal total) => Math.Floor(total / 10m) * 10m;

    /// <summary>
    /// 打折：1～9 代表幾折（9 → 90%），10～99 代表幾幾折（85 → 85%），結果四捨五入到元。
    /// 超出範圍回傳 null。
    /// </summary>
    public static decimal? ApplyRate(decimal total, int rate)
    {
        var percent = rate switch
        {
            >= 1 and <= 9 => rate * 10,
            >= 10 and <= 99 => rate,
            _ => 0
        };
        return percent == 0 ? null : Math.Round(total * percent / 100m, MidpointRounding.AwayFromZero);
    }

    public static bool RequiresNote(DiscountReason reason) =>
        reason is DiscountReason.FoodIssue or DiscountReason.Other;

    /// <summary>檢查折扣後應收金額與原因，回傳錯誤訊息；null 代表可以套用。</summary>
    public static string? Validate(decimal total, decimal dueAmount, DiscountReason? reason, string? note)
    {
        if (dueAmount != decimal.Truncate(dueAmount))
        {
            return "折扣後金額需為整數。";
        }
        if (dueAmount < 0)
        {
            return "折扣後金額不可小於 0。";
        }
        if (dueAmount >= total)
        {
            return "折扣後金額需小於原價。";
        }
        if (reason is null || !Enum.IsDefined(reason.Value))
        {
            return "請選擇折扣原因。";
        }
        if (RequiresNote(reason.Value) && string.IsNullOrWhiteSpace(note))
        {
            return $"折扣原因為「{reason.Value.ToDisplayText()}」時，請填寫說明。";
        }
        if (note is { Length: > MaxNoteLength })
        {
            return $"折扣說明不可超過 {MaxNoteLength} 字。";
        }
        return null;
    }
}
