using ManTingEats.Controllers;
using ManTingEats.Models;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using ManTingEats.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Xunit;

namespace ManTingEats.Tests;

public sealed class ExpenseControllerTests : IDisposable
{
    // 沿用 OrderController 的測試環境，只用到其中的資料庫
    private readonly OrderControllerTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private ExpenseController CreateController()
    {
        var httpContext = new DefaultHttpContext();
        return new ExpenseController(_host.CreateDbContext())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new OrderControllerTestHost.NullTempDataProvider())
        };
    }

    private static CreateExpenseCommand Command(DateTime date) => new(date, ExpenseCategory.Ingredients, 500m, null);

    [Fact]
    public void Create_Get_DefaultsToBusinessToday()
    {
        // 凌晨記帳時應帶入前一晚的營業日（v12），報表才會與同晚的營收對上
        var result = CreateController().Create();

        var command = Assert.IsType<CreateExpenseCommand>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(TaipeiTime.BusinessToday, command.Date);
    }

    [Fact]
    public async Task Create_BusinessToday_Saves()
    {
        var result = await CreateController().Create(Command(TaipeiTime.BusinessToday));

        Assert.IsType<RedirectToActionResult>(result);
        using var db = _host.CreateDbContext();
        Assert.Equal(TaipeiTime.BusinessToday, Assert.Single(db.Expenses).Date);
    }

    [Fact]
    public async Task Create_AfterBusinessToday_ReturnsError()
    {
        var controller = CreateController();

        var result = await controller.Create(Command(TaipeiTime.BusinessToday.AddDays(1)));

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        using var db = _host.CreateDbContext();
        Assert.Empty(db.Expenses);
    }

    [Fact]
    public async Task Index_PagedByTen_TotalCoversAllPages()
    {
        using (var db = _host.CreateDbContext())
        {
            for (var i = 1; i <= 12; i++)
            {
                db.Expenses.Add(new Expense { Date = TaipeiTime.BusinessToday.AddDays(-i), Category = ExpenseCategory.Other, Amount = 100m });
            }
            db.SaveChanges();
        }

        var result = await CreateController().Index(null, null, null, page: 2);

        var model = Assert.IsType<ExpenseSearchViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(2, model.Results.TotalPages);
        Assert.Equal(2, model.Results.Items.Count);
        Assert.Equal(1200m, model.TotalAmount);
    }
}
