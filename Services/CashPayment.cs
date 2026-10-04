namespace ManTingEats.Services;

/// <summary>結帳收款（僅現金）的計算規則，見 docs/prd/v6.md；收款金額只在畫面計算、不存資料庫。</summary>
public static class CashPayment
{
    /// <summary>收款金額上限，與支出等金額欄位同樣防止誤觸多打幾個 0。</summary>
    public const decimal MaxReceivedAmount = 100_000m;

    private static readonly decimal[] CommonBills = [500m, 1000m];

    /// <summary>
    /// 快速金額按鈕：下一個整百、$500、$1000 中「大於應收」者，由小到大且不重複。
    /// 等於應收的面額不列出，因為已有「收剛好」按鈕。
    /// </summary>
    public static List<decimal> QuickAmounts(decimal total)
    {
        var nextHundred = (Math.Floor(total / 100m) + 1) * 100m;
        return CommonBills
            .Prepend(nextHundred)
            .Where(amount => amount > total)
            .Distinct()
            .Order()
            .ToList();
    }

    /// <summary>檢查收款金額，回傳錯誤訊息；null 代表可以結帳。</summary>
    public static string? Validate(decimal? received, decimal total)
    {
        if (received is null)
        {
            return "請選擇「收剛好」或輸入收到的金額。";
        }
        if (received != decimal.Truncate(received.Value))
        {
            return "收到金額需為整數。";
        }
        if (received > MaxReceivedAmount)
        {
            return $"收到金額不可超過 ${MaxReceivedAmount:N0}。";
        }
        if (received < total)
        {
            return $"收到金額不足，還差 ${total - received.Value:F0}。";
        }
        return null;
    }
}
