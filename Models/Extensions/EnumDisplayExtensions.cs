using ManTingEats.Models.Enums;

namespace ManTingEats.Models.Extensions;

public static class EnumDisplayExtensions
{
    public static string ToDisplayText(this OrderChannel channel) => channel switch
    {
        OrderChannel.DineIn => "內用",
        OrderChannel.Takeout => "外帶",
        _ => channel.ToString()
    };

    public static string ToDisplayText(this OrderStatus status) => status switch
    {
        OrderStatus.Open => "未結帳",
        OrderStatus.Completed => "已結帳",
        OrderStatus.Voided => "已作廢",
        _ => status.ToString()
    };
}
