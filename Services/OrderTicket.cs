using ManTingEats.Models.Entities;

namespace ManTingEats.Services;

/// <summary>出單（PassPRNT）流程中與資料庫無關的判斷邏輯，獨立出來以便單元測試。</summary>
public static class OrderTicket
{
    /// <summary>尚未出單的品項；確認訂單時就是印這一批。</summary>
    public static List<OrderItem> PendingItems(Order order) =>
        order.Items.Where(i => !i.IsPrinted).ToList();

    /// <summary>訂單從未出過單時印「全單」，否則印只含新品項的「加點單」。</summary>
    public static bool IsFirstBatch(Order order) =>
        order.Items.All(i => !i.IsPrinted);

    /// <summary>
    /// 將送印當下記錄的品項標記為已出單，回傳實際標記的數量。
    /// 只認送印時帶出去的 ID：列印期間若有人加點，新品項不在名單內，不會被誤標為已出單；
    /// 送印後才被移除的品項則自然找不到而略過。
    /// </summary>
    public static int MarkPrinted(Order order, IEnumerable<int> itemIds)
    {
        var ids = itemIds.ToHashSet();
        var marked = 0;
        foreach (var item in order.Items.Where(i => ids.Contains(i.Id) && !i.IsPrinted))
        {
            item.IsPrinted = true;
            marked++;
        }
        return marked;
    }

    /// <summary>品項 ID 以逗號串在 PassPRNT 回呼網址中；無法解析的片段直接略過，不讓竄改過的網址造成例外。</summary>
    public static string FormatItemIds(IEnumerable<OrderItem> items) =>
        string.Join(',', items.Select(i => i.Id));

    public static List<int> ParseItemIds(string? value) =>
        (value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
            .OfType<int>()
            .ToList();
}
