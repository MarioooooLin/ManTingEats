using System.ComponentModel.DataAnnotations;
using ManTingEats.Models.Validation;

namespace ManTingEats.Models.Commands;

/// <summary>完成日結（v13）：營收、支出等數字由伺服器重新計算，不信任表單送來的值。</summary>
public sealed record CloseDayCommand(
    [property: Display(Name = "營業日")][Required(ErrorMessage = "請選擇營業日")] DateTime BusinessDate,
    [property: Display(Name = "零用金")][Range(typeof(decimal), "0", "1000000", ErrorMessage = "零用金需介於 0 到 1,000,000")][WholeAmount] decimal OpeningCash,
    [property: Display(Name = "實點現金")][Required(ErrorMessage = "請輸入實點現金")][Range(typeof(decimal), "0", "10000000", ErrorMessage = "實點現金需介於 0 到 10,000,000")][WholeAmount] decimal? CountedCash,
    [property: Display(Name = "備註")][StringLength(200, ErrorMessage = "備註不得超過 200 字")] string? Note);
