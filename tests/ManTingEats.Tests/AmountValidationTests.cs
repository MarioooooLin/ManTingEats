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
}
