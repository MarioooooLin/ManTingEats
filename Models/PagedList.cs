using Microsoft.EntityFrameworkCore;

namespace ManTingEats.Models;

/// <summary>分頁導覽列（_Pagination）只需要頁碼資訊，不需知道資料型別，故另立非泛型介面。</summary>
public interface IPagination
{
    int Page { get; }
    int TotalPages { get; }
    int TotalCount { get; }
}

public sealed class PagedList<T> : IPagination
{
    /// <summary>全站列表每頁筆數（v12 由 20 改為 10，使用者要求訂單、支出、訂位列表一致）。</summary>
    public const int DefaultPageSize = 10;

    public required List<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    /// <summary>
    /// query 需已排序，且排序須唯一（例如最後加上 Id），否則同值資料在換頁時可能重複或漏掉。
    /// 頁碼超出範圍時夾回第一頁／最後一頁，避免手改網址或資料被刪後看到空白頁。
    /// </summary>
    public static async Task<PagedList<T>> CreateAsync(IQueryable<T> query, int page, int pageSize = DefaultPageSize)
    {
        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedList<T> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }
}
