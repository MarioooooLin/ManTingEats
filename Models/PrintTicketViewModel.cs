using ManTingEats.Models.Entities;

namespace ManTingEats.Models;

/// <summary>PassPRNT 出單頁的資料：出單內容（版面見 Views/Order/_Ticket.cshtml）與印完的回呼路徑。</summary>
public sealed class PrintTicketViewModel
{
    public required Order Order { get; init; }

    /// <summary>出單標題：全　單／加　點／補　印。</summary>
    public required string Title { get; init; }

    /// <summary>這張單要列出的品項；加點單只列本次新增，全單與補印列全部。</summary>
    public required IReadOnlyList<OrderItem> Items { get; init; }

    public required string TotalLabel { get; init; }

    /// <summary>台灣時間；容器時區為 UTC，不可直接用 DateTime.Now。</summary>
    public required DateTime PrintedAt { get; init; }

    /// <summary>PassPRNT 印完要回到的站內路徑（相對路徑，由前端補上目前網域，避免反向代理後主機名稱判斷錯誤）。</summary>
    public required string ResultPath { get; init; }
}
