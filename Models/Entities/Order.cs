using ManTingEats.Models.Enums;

namespace ManTingEats.Models.Entities;

public sealed class Order
{
    public int Id { get; set; }
    public OrderChannel Channel { get; set; }

    /// <summary>當日流水號（以 Asia/Taipei 時區為每日邊界），僅供人員溝通顯示用，非資料庫主鍵。</summary>
    public int DailyNumber { get; set; }

    /// <summary>內用必填，外帶留空；用於加點時辨識訂單對象，非正式座位管理。</summary>
    public string? TableNumber { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Open;

    /// <summary>品項＋加料的原價合計；折扣另記於 DiscountAmount，實收請用 AmountDue。</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>結帳時給的折扣金額（v8），未折扣為 0。</summary>
    public decimal DiscountAmount { get; set; }
    public DiscountReason? DiscountReason { get; set; }
    public string? DiscountNote { get; set; }
    public int? DiscountedByEmployeeId { get; set; }
    public Employee? DiscountedByEmployee { get; set; }

    /// <summary>實收金額 = 原價 − 折扣；營收報表以此計算。唯讀屬性不對應資料庫欄位。</summary>
    public decimal AmountDue => TotalAmount - DiscountAmount;

    public int CreatedByEmployeeId { get; set; }
    public Employee? CreatedByEmployee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public string? VoidReason { get; set; }
    public int? VoidedByEmployeeId { get; set; }
    public Employee? VoidedByEmployee { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
