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
    public decimal TotalAmount { get; set; }

    public int CreatedByEmployeeId { get; set; }
    public Employee? CreatedByEmployee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public string? VoidReason { get; set; }
    public int? VoidedByEmployeeId { get; set; }
    public Employee? VoidedByEmployee { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
