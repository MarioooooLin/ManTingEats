using System.Net.Sockets;
using System.Text;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Extensions;
using ManTingEats.Models.Options;
using Microsoft.Extensions.Options;

namespace ManTingEats.Services;

/// <summary>透過 LAN 直連印表機（Star mC-Print3, TCP:9100 raw socket），內容以 Big5 編碼送出。</summary>
public sealed class LanReceiptPrinterService : IReceiptPrinterService
{
    // 印表機字元模式為 Big5（實機自我測試頁確認），.NET 需先註冊 CodePagesEncodingProvider 才能取得此編碼
    private static readonly Encoding TicketEncoding = Encoding.GetEncoding(950);

    private readonly PrinterOptions _options;
    private readonly ILogger<LanReceiptPrinterService> _logger;

    public LanReceiptPrinterService(IOptions<PrinterOptions> options, ILogger<LanReceiptPrinterService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<bool> PrintNewOrderAsync(Order order, CancellationToken cancellationToken = default)
        => SendAsync(order.Id, "全單", BuildFullOrderTicket(order), cancellationToken);

    public Task<bool> PrintAddedItemsAsync(Order order, IReadOnlyList<OrderItem> addedItems, CancellationToken cancellationToken = default)
        => SendAsync(order.Id, "加點單", BuildAddedItemsTicket(order, addedItems), cancellationToken);

    public Task<bool> PrintVoidNoticeAsync(Order order, CancellationToken cancellationToken = default)
        => SendAsync(order.Id, "作廢通知", BuildVoidTicket(order), cancellationToken);

    private async Task<bool> SendAsync(int orderId, string ticketName, string content, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return true;
        }

        try
        {
            using var client = new TcpClient();
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(_options.TimeoutMs);
            await client.ConnectAsync(_options.Host, _options.Port, connectCts.Token);

            using var stream = client.GetStream();
            var bytes = TicketEncoding.GetBytes(content);
            await stream.WriteAsync(bytes, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "訂單 {OrderId} 的{TicketName}列印失敗，印表機 {Host}:{Port}", orderId, ticketName, _options.Host, _options.Port);
            return false;
        }
    }

    private static string BuildFullOrderTicket(Order order)
    {
        var sb = new StringBuilder();
        AppendHeader(sb, "全   單", order);
        foreach (var item in order.Items)
        {
            AppendItemLine(sb, item);
        }
        AppendTotalFooter(sb, order.TotalAmount);
        return sb.ToString();
    }

    private static string BuildAddedItemsTicket(Order order, IReadOnlyList<OrderItem> addedItems)
    {
        var sb = new StringBuilder();
        AppendHeader(sb, "加   點", order);
        foreach (var item in addedItems)
        {
            AppendItemLine(sb, item);
        }
        AppendTotalFooter(sb, order.TotalAmount, "訂單目前總金額");
        return sb.ToString();
    }

    private static string BuildVoidTicket(Order order)
    {
        var sb = new StringBuilder();
        AppendHeader(sb, "作廢通知", order);
        sb.AppendLine("請立即停止製作以下品項：");
        sb.AppendLine("------------------------");
        foreach (var item in order.Items)
        {
            sb.AppendLine($"{item.MenuItem?.Name} x{item.Quantity}");
        }
        sb.AppendLine("========================");
        sb.AppendLine();
        sb.AppendLine();
        return sb.ToString();
    }

    private static void AppendHeader(StringBuilder sb, string title, Order order)
    {
        sb.AppendLine("========================");
        sb.AppendLine(title);
        sb.AppendLine("========================");
        sb.AppendLine($"訂單編號：#{order.DailyNumber}");
        sb.AppendLine($"通路：{order.Channel.ToDisplayText()}");
        sb.AppendLine($"桌號：{order.TableNumber ?? "-"}");
        sb.AppendLine($"時間：{DateTime.Now:yyyy/MM/dd HH:mm}");
        sb.AppendLine("------------------------");
    }

    private static void AppendItemLine(StringBuilder sb, OrderItem item)
    {
        sb.AppendLine($"{item.MenuItem?.Name} x{item.Quantity}  單價${item.UnitPrice:F0}  小計${item.UnitPrice * item.Quantity:F0}");
        if (!string.IsNullOrWhiteSpace(item.Note))
        {
            sb.AppendLine($"  備註：{item.Note}");
        }
    }

    private static void AppendTotalFooter(StringBuilder sb, decimal totalAmount, string label = "總金額")
    {
        sb.AppendLine("------------------------");
        sb.AppendLine($"{label}：${totalAmount:F0}");
        sb.AppendLine("========================");
        sb.AppendLine();
        sb.AppendLine();
    }
}
