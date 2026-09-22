namespace ManTingEats.Models.Options;

/// <summary>對應 appsettings.json 的 "Printer" 設定區塊。</summary>
public sealed class PrinterOptions
{
    public const string SectionName = "Printer";

    /// <summary>關閉時所有列印呼叫直接視為成功並略過，供無實體印表機的環境（開發/測試）使用。</summary>
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 9100;
    public int TimeoutMs { get; set; } = 3000;
}
