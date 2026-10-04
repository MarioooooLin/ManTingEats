namespace ManTingEats.Models;

/// <summary>PassPRNT 回報列印成功後，自動送出「標記已出單」所需的資料。</summary>
public sealed record PrintSucceededViewModel(int OrderId, string Items);
