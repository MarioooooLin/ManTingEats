using ManTingEats.Models.Entities;

namespace ManTingEats.Services;

/// <summary>出單列印服務，對應 docs/prd/v3.md 規格；列印失敗不應阻擋訂單操作，呼叫端僅需依回傳值提示使用者。</summary>
public interface IReceiptPrinterService
{
    /// <summary>建單後第一次加點時列印「全單」。</summary>
    Task<bool> PrintNewOrderAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>後續加點時僅列印本次新增的品項。</summary>
    Task<bool> PrintAddedItemsAsync(Order order, IReadOnlyList<OrderItem> addedItems, CancellationToken cancellationToken = default);
}
