using System.ComponentModel.DataAnnotations;
using ManTingEats.Models.Validation;

namespace ManTingEats.Models.Commands;

/// <summary>
/// 完成日結（v13）：營收、支出等數字由伺服器重新計算，不信任表單送來的值。
/// 外送平台的數字（v18）系統無從得知，只能採用店家照平台後台抄的值；留空視為 0（當天沒開外送或沒單）。
/// </summary>
public sealed record CloseDayCommand(
    [property: Display(Name = "營業日")][Required(ErrorMessage = "請選擇營業日")] DateTime BusinessDate,
    [property: Display(Name = "零用金")][Range(typeof(decimal), "0", "1000000", ErrorMessage = "零用金需介於 0 到 1,000,000")][WholeAmount] decimal OpeningCash,
    [property: Display(Name = "實點現金")][Required(ErrorMessage = "請輸入實點現金")][Range(typeof(decimal), "0", "10000000", ErrorMessage = "實點現金需介於 0 到 10,000,000")][WholeAmount] decimal? CountedCash,
    [property: Display(Name = "備註")][StringLength(200, ErrorMessage = "備註不得超過 200 字")] string? Note,
    [property: Display(Name = "Uber Eats 訂單數")][Range(0, 10000, ErrorMessage = "訂單數需介於 0 到 10,000")] int? UberEatsOrderCount = null,
    [property: Display(Name = "Uber Eats 營業額")][Range(typeof(decimal), "0", "10000000", ErrorMessage = "營業額需介於 0 到 10,000,000")][WholeAmount] decimal? UberEatsSales = null,
    [property: Display(Name = "Uber Eats 實收")][Range(typeof(decimal), "0", "10000000", ErrorMessage = "實收需介於 0 到 10,000,000")][WholeAmount] decimal? UberEatsPayout = null,
    [property: Display(Name = "foodpanda 訂單數")][Range(0, 10000, ErrorMessage = "訂單數需介於 0 到 10,000")] int? FoodpandaOrderCount = null,
    [property: Display(Name = "foodpanda 營業額")][Range(typeof(decimal), "0", "10000000", ErrorMessage = "營業額需介於 0 到 10,000,000")][WholeAmount] decimal? FoodpandaSales = null,
    [property: Display(Name = "foodpanda 實收")][Range(typeof(decimal), "0", "10000000", ErrorMessage = "實收需介於 0 到 10,000,000")][WholeAmount] decimal? FoodpandaPayout = null);
