using System.ComponentModel.DataAnnotations;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Enums;
using ManTingEats.Services;
using Xunit;

namespace ManTingEats.Tests;

public class AmountValidationTests
{
    /// <summary>
    /// Command 皆為 positional record，驗證屬性掛在建構子參數上（MVC 會讀取），
    /// Validator.TryValidateObject 只看屬性會漏掉，故比照 MVC 逐一套用參數上的驗證屬性。
    /// </summary>
    private static List<ValidationResult> Validate(object model)
    {
        var type = model.GetType();
        var constructor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var results = new List<ValidationResult>();
        foreach (var parameter in constructor.GetParameters())
        {
            var value = type.GetProperty(parameter.Name!)!.GetValue(model);
            var context = new ValidationContext(model) { MemberName = parameter.Name };
            var attributes = parameter.GetCustomAttributes(typeof(ValidationAttribute), inherit: true).Cast<ValidationAttribute>();
            Validator.TryValidateValue(value!, context, results, attributes);
        }
        return results;
    }

    [Theory]
    [InlineData("1")]
    [InlineData("85.00")] // 資料庫 decimal(10,2) 讀回的整數金額帶兩位小數，編輯時原值送出不可被擋
    [InlineData("100000")]
    public void MenuItemPrice_WholeAmountWithinRange_IsValid(string price)
    {
        var command = new CreateMenuItemCommand("牛肉麵", MenuCategory.Food, decimal.Parse(price));

        Assert.Empty(Validate(command));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("85.5")]
    [InlineData("100001")]
    public void MenuItemPrice_FractionalOrOutOfRange_IsInvalid(string price)
    {
        var command = new CreateMenuItemCommand("牛肉麵", MenuCategory.Food, decimal.Parse(price));

        Assert.NotEmpty(Validate(command));
    }

    [Fact]
    public void AddOnPrice_Zero_IsValid()
    {
        Assert.Empty(Validate(new CreateAddOnCommand("加蔥", 0)));
    }

    [Theory]
    [InlineData("10.5")]
    [InlineData("10001")]
    public void AddOnPrice_FractionalOrOutOfRange_IsInvalid(string price)
    {
        Assert.NotEmpty(Validate(new CreateAddOnCommand("加蛋", decimal.Parse(price))));
    }

    [Theory]
    [InlineData("10000000", true)]
    [InlineData("10000001", false)]
    [InlineData("99.9", false)]
    public void ExpenseAmount_Limits(string amount, bool expectedValid)
    {
        var command = new CreateExpenseCommand(DateTime.Today, ExpenseCategory.Rent, decimal.Parse(amount), null);

        Assert.Equal(expectedValid, Validate(command).Count == 0);
    }

    [Theory]
    [InlineData(99, true)]
    [InlineData(100, false)]
    public void OrderItemQuantity_Limits(int quantity, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validate(new AddOrderItemCommand(1, quantity)).Count == 0);
    }

    [Theory]
    [InlineData("12", true)]
    [InlineData("A3", true)]
    [InlineData("外帶區1", true)]
    [InlineData("1234567890", true)]
    [InlineData("12345678901", false)]
    public void TableNumber_MaxTenCharacters(string tableNumber, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validate(new CreateOrderCommand(OrderChannel.DineIn, tableNumber)).Count == 0);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(6, true)]
    [InlineData(-1, false)]
    [InlineData(7, false)]
    public void SpiceLevel_ZeroToSix(int spiceLevel, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validate(new AddOrderItemCommand(1, 1, SpiceLevel: spiceLevel)).Count == 0);
    }

    [Theory]
    [InlineData(50, true)]
    [InlineData(51, false)]
    public void ReservationPartySize_Limits(int partySize, bool expectedValid)
    {
        var command = new CreateReservationCommand("王小明", "0912345678", partySize, DateTime.Today.AddDays(1));

        Assert.Equal(expectedValid, Validate(command).Count == 0);
    }

    [Fact]
    public void TaipeiTime_StartOfDayUtc_IsPreviousDay1600Utc()
    {
        var startUtc = TaipeiTime.StartOfDayUtc(new DateTime(2026, 9, 29));

        Assert.Equal(new DateTime(2026, 9, 28, 16, 0, 0), startUtc);
    }

    // 營業 17:00–02:00 跨午夜：06:00 前仍屬前一天的營業日，單號與報表才不會在午夜被切開（v9）
    [Theory]
    [InlineData(2026, 10, 8, 17, 0, 8)]
    [InlineData(2026, 10, 8, 23, 59, 8)]
    [InlineData(2026, 10, 9, 0, 0, 8)]
    [InlineData(2026, 10, 9, 1, 30, 8)]
    [InlineData(2026, 10, 9, 5, 59, 8)]
    [InlineData(2026, 10, 9, 6, 0, 9)]
    public void TaipeiTime_BusinessDateOf_SwitchesAt0600(int year, int month, int day, int hour, int minute, int expectedDay)
    {
        var businessDate = TaipeiTime.BusinessDateOf(new DateTime(year, month, day, hour, minute, 0));

        Assert.Equal(new DateTime(2026, 10, expectedDay), businessDate);
    }

    [Fact]
    public void TaipeiTime_BusinessDayStartUtc_IsSameDay2200UtcOfPreviousDay()
    {
        // 台灣 10/8 06:00 = UTC 10/7 22:00
        var startUtc = TaipeiTime.BusinessDayStartUtc(new DateTime(2026, 10, 8));

        Assert.Equal(new DateTime(2026, 10, 7, 22, 0, 0), startUtc);
    }
}
