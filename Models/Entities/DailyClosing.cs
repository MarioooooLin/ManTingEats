namespace ManTingEats.Models.Entities;

/// <summary>
/// 日結紀錄（v13）：打烊時點收現金並與系統金額對帳。所有數字都是日結當下的快照，
/// 之後訂單或支出再變動也不影響舊紀錄，差額的歷史才查得清楚。
/// 同一營業日可重新日結，每次都新增一筆，以最後一筆為準。
/// </summary>
public sealed class DailyClosing
{
    public int Id { get; set; }

    /// <summary>營業日（06:00 切換，v9），只存日期。</summary>
    public DateTime BusinessDate { get; set; }

    /// <summary>開店時收銀機與抽屜的零用金。</summary>
    public decimal OpeningCash { get; set; }

    /// <summary>當日營收（已結帳訂單實收，與營收報表算法相同）。</summary>
    public decimal Revenue { get; set; }

    /// <summary>當日支出合計；支出皆從抽屜付現金。</summary>
    public decimal ExpenseTotal { get; set; }

    public decimal DiscountTotal { get; set; }
    public int CompletedOrderCount { get; set; }
    public int VoidedOrderCount { get; set; }

    /// <summary>實際點算的現金總額。</summary>
    public decimal CountedCash { get; set; }

    // 外送平台（v18）：外送單在各平台自己的 App 處理、金流不經過本系統，日結時照平台商家後台手動抄當日總額；
    // 營業額為顧客付的金額，實收為平台扣抽成與各項費用後撥給店家的金額。沒填視為 0。
    public int UberEatsOrderCount { get; set; }
    public decimal UberEatsSales { get; set; }
    public decimal UberEatsPayout { get; set; }
    public int FoodpandaOrderCount { get; set; }
    public decimal FoodpandaSales { get; set; }
    public decimal FoodpandaPayout { get; set; }

    /// <summary>差額說明，重新日結時必填原因。</summary>
    public string? Note { get; set; }

    public int ClosedByEmployeeId { get; set; }
    public Employee? ClosedByEmployee { get; set; }
    public DateTime ClosedAt { get; set; } = DateTime.UtcNow;

    /// <summary>應有現金 = 零用金 + 營收 − 支出（目前只收現金、支出皆付現金）。唯讀屬性不對應資料庫欄位。</summary>
    public decimal ExpectedCash => OpeningCash + Revenue - ExpenseTotal;

    /// <summary>差額 = 實點 − 應有；正數為多、負數為少。</summary>
    public decimal Difference => CountedCash - ExpectedCash;

    /// <summary>外送營業額合計（v18）；外送款不進收銀機，不影響 <see cref="ExpectedCash"/>。</summary>
    public decimal DeliverySales => UberEatsSales + FoodpandaSales;

    public decimal DeliveryPayout => UberEatsPayout + FoodpandaPayout;

    /// <summary>店內＋外送營業額合計（v18）。</summary>
    public decimal TotalSales => Revenue + DeliverySales;
}
