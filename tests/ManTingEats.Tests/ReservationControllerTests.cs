using ManTingEats.Controllers;
using ManTingEats.Models;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using ManTingEats.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManTingEats.Tests;

public sealed class ReservationControllerTests : IDisposable
{
    // 借用 OrderController 測試環境的 SQLite 記憶體資料庫；訂位不依賴其中的菜單資料
    private readonly OrderControllerTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private ReservationController CreateController()
    {
        var httpContext = new DefaultHttpContext();
        return new ReservationController(_host.CreateDbContext())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new OrderControllerTestHost.NullTempDataProvider())
        };
    }

    private int SeedReservation(DateTime reservedAt)
    {
        using var db = _host.CreateDbContext();
        var reservation = new Reservation { CustomerName = "王小明", PhoneNumber = "0912345678", PartySize = 2, ReservedAt = reservedAt };
        db.Reservations.Add(reservation);
        db.SaveChanges();
        return reservation.Id;
    }

    private Reservation? LoadReservation(int id)
    {
        using var db = _host.CreateDbContext();
        return db.Reservations.AsNoTracking().SingleOrDefault(r => r.Id == id);
    }

    [Fact]
    public async Task Edit_UpdatesAllFields()
    {
        var id = SeedReservation(TaipeiTime.Now.AddDays(1));
        var newTime = TaipeiTime.Now.AddDays(2);

        var result = await CreateController().Edit(new UpdateReservationCommand(id, "陳大文", "0987654321", 4, newTime));

        Assert.IsType<RedirectToActionResult>(result);
        var saved = LoadReservation(id)!;
        Assert.Equal("陳大文", saved.CustomerName);
        Assert.Equal("0987654321", saved.PhoneNumber);
        Assert.Equal(4, saved.PartySize);
        Assert.Equal(newTime, saved.ReservedAt);
    }

    [Fact]
    public async Task Edit_PastReservationWithSameTime_CanStillCorrectDetails()
    {
        var pastTime = TaipeiTime.Now.AddDays(-3);
        var id = SeedReservation(pastTime);

        var result = await CreateController().Edit(new UpdateReservationCommand(id, "王小明", "0911111111", 2, pastTime));

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("0911111111", LoadReservation(id)!.PhoneNumber);
    }

    [Fact]
    public async Task Edit_MoveToPastTime_IsRejected()
    {
        var originalTime = TaipeiTime.Now.AddDays(1);
        var id = SeedReservation(originalTime);
        var controller = CreateController();

        var result = await controller.Edit(new UpdateReservationCommand(id, "王小明", "0912345678", 2, TaipeiTime.Now.AddHours(-1)));

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(originalTime, LoadReservation(id)!.ReservedAt);
    }

    [Fact]
    public async Task Edit_UnknownId_ReturnsNotFound()
    {
        var result = await CreateController().Edit(new UpdateReservationCommand(999, "王小明", "0912345678", 2, TaipeiTime.Now.AddDays(1)));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_RemovesReservation()
    {
        var id = SeedReservation(TaipeiTime.Now.AddDays(1));

        var result = await CreateController().Delete(id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(LoadReservation(id));
    }

    private async Task<ReservationSearchViewModel> LoadIndex(string? customerName = null, DateTime? date = null)
    {
        var result = await CreateController().Index(customerName, date);
        return Assert.IsType<ReservationSearchViewModel>(Assert.IsType<ViewResult>(result).Model);
    }

    [Fact]
    public async Task Index_NoFilter_ShowsOnlyFromTodayInTimeOrder()
    {
        var today = TaipeiTime.Today;
        SeedReservation(today.AddDays(-1).AddHours(18));
        var later = SeedReservation(today.AddDays(2).AddHours(18));
        var earlierToday = SeedReservation(today.AddHours(1));    // 今天稍早（可能已過）仍要列出

        var model = await LoadIndex();

        Assert.True(model.UpcomingOnly);
        Assert.Equal([earlierToday, later], model.Results.Items.Select(r => r.Id).ToList());
    }

    [Fact]
    public async Task Index_NameSearch_IncludesPastNewestFirst()
    {
        var today = TaipeiTime.Today;
        var past = SeedReservation(today.AddDays(-30));
        var upcoming = SeedReservation(today.AddDays(3));

        var model = await LoadIndex(customerName: "小明");

        Assert.False(model.UpcomingOnly);
        Assert.Equal([upcoming, past], model.Results.Items.Select(r => r.Id).ToList());
    }

    [Fact]
    public async Task Index_DateSearch_CanShowPastDay()
    {
        var pastDay = TaipeiTime.Today.AddDays(-10);
        var id = SeedReservation(pastDay.AddHours(18));
        SeedReservation(TaipeiTime.Today.AddDays(1));

        var model = await LoadIndex(date: pastDay);

        Assert.Equal([id], model.Results.Items.Select(r => r.Id).ToList());
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await CreateController().Delete(999));
    }
}
