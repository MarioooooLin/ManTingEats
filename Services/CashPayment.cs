namespace ManTingEats.Services;

/// <summary>
/// 結帳收款（僅現金）的檢查規則，見 docs/prd/v6.md；收款金額只在畫面計算、不存資料庫。
/// 快速金額按鈕需隨折扣後應收（v8）即時變動，改由 Views/Order/Details.cshtml 的前端腳本計算。
/// </summary>
public static class CashPayment
{
    /// <summary>收款金額上限，與支出等金額欄位同樣防止誤觸多打幾個 0。</summary>
    public const decimal MaxReceivedAmount = 100_000m;

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
