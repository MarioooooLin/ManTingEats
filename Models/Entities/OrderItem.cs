namespace ManTingEats.Models.Entities;

public sealed class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    /// <summary>下單當下的售價快照，避免菜單日後調價影響歷史訂單金額。</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>份數。客製化品項（v10 起）可點多份，同一行的辣度、加料、備註套用到每一份；設定不同需分行加點。</summary>
    public int Quantity { get; set; }

    /// <summary>辣度 0～6，僅品項開放辣度時使用，null 代表該品項不可調辣度。原為五級列舉，改為 int 後資料庫欄位型別不變。</summary>
    public int? SpiceLevel { get; set; }

    /// <summary>給廚房看的單項備註（例：少辣、不要葱），選填。</summary>
    public string? Note { get; set; }

    /// <summary>是否已隨「確認出單」列印過，避免每次加點即時列印造成亂印。</summary>
    public bool IsPrinted { get; set; }

    /// <summary>加料的數量為「每份」的量（v10），加料總額需乘以份數。</summary>
    public ICollection<OrderItemAddOn> AddOns { get; set; } = new List<OrderItemAddOn>();

    /// <summary>每份的加料金額。唯讀屬性不對應資料庫欄位。</summary>
    public decimal AddOnsPricePerServing => AddOns.Sum(a => a.UnitPrice * a.Quantity);

    /// <summary>整行的加料總額 = 每份加料金額 x 份數；訂單總額、出單、報表都用這個算法，避免各處漏乘份數。</summary>
    public decimal AddOnsTotal => AddOnsPricePerServing * Quantity;

    /// <summary>整行小計 = 品項金額 + 加料總額。</summary>
    public decimal Subtotal => UnitPrice * Quantity + AddOnsTotal;
}
