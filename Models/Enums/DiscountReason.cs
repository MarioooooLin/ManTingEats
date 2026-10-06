namespace ManTingEats.Models.Enums;

/// <summary>結帳折扣原因（v8），固定選項以便一鍵選擇；「餐點問題」「其他」需另填說明。</summary>
public enum DiscountReason
{
    RegularCustomer,
    RoundOff,
    Treat,
    FoodIssue,
    Other
}
