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

    public static string ToDisplayText(this ExpenseCategory category) => category switch
    {
        ExpenseCategory.Ingredients => "食材採購",
        ExpenseCategory.Utilities => "水電瓦斯",
        ExpenseCategory.Rent => "房租",
        ExpenseCategory.Payroll => "人事薪資",
        ExpenseCategory.Equipment => "設備/耗材",
        ExpenseCategory.Other => "其他",
        _ => category.ToString()
    };

    public static string ToDisplayText(this MenuCategory category) => category switch
    {
        MenuCategory.Food => "吃",
        MenuCategory.Drink => "喝",
        MenuCategory.Other => "其他",
        _ => category.ToString()
    };
}
