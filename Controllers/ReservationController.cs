using ManTingEats.Data;
using ManTingEats.Models;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using ManTingEats.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManTingEats.Controllers;

[Authorize]
public sealed class ReservationController : Controller
{
    private readonly AppDbContext _db;

    public ReservationController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? customerName, DateTime? date, int page = 1)
    {
        var query = _db.Reservations.AsQueryable();
        var hasName = !string.IsNullOrWhiteSpace(customerName);

        if (hasName)
        {
            query = query.Where(r => r.CustomerName.Contains(customerName!));
        }

        if (date.HasValue)
        {
            var start = date.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(r => r.ReservedAt >= start && r.ReservedAt < end);
        }

        // 未指定姓名與日期時只列今天以後的訂位，否則資料累積後一打開就先看到很久以前的訂位。
        // 依姓名查詢則涵蓋過去的訂位：顧客請求刪除個資時要能找到舊資料。ReservedAt 存台灣當地時間，故以台灣日期比對
        var upcomingOnly = !hasName && !date.HasValue;
        if (upcomingOnly)
        {
            var today = TaipeiTime.Today;
            query = query.Where(r => r.ReservedAt >= today);
        }

        // 只依姓名查詢時可能橫跨多年，最近的排前面；其餘情況依時段先後，方便看接下來的訂位
        var ordered = hasName && !date.HasValue
            ? query.OrderByDescending(r => r.ReservedAt).ThenByDescending(r => r.Id)
            : query.OrderBy(r => r.ReservedAt).ThenBy(r => r.Id);

        return View(new ReservationSearchViewModel
        {
            CustomerName = customerName,
            Date = date,
            UpcomingOnly = upcomingOnly,
            Results = await PagedList<Reservation>.CreateAsync(ordered, page)
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateReservationCommand(string.Empty, string.Empty, 1, DateTime.Today.AddHours(18)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReservationCommand command)
    {
        ValidateReservedAt(command.ReservedAt, nameof(command.ReservedAt));
        if (!ModelState.IsValid)
        {
            return View(command);
        }

        _db.Reservations.Add(new Reservation
        {
            CustomerName = command.CustomerName,
            PhoneNumber = command.PhoneNumber,
            PartySize = command.PartySize,
            ReservedAt = command.ReservedAt
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "訂位建立成功。";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var reservation = await _db.Reservations.FindAsync(id);
        if (reservation is null)
        {
            return NotFound();
        }

        return View(new UpdateReservationCommand(reservation.Id, reservation.CustomerName, reservation.PhoneNumber, reservation.PartySize, reservation.ReservedAt));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateReservationCommand command)
    {
        var reservation = await _db.Reservations.FindAsync(command.Id);
        if (reservation is null)
        {
            return NotFound();
        }

        // 時段沒改就不檢查：已過的訂位仍要能更正姓名、電話等資料（例如顧客請求更正個資）
        if (command.ReservedAt != reservation.ReservedAt)
        {
            ValidateReservedAt(command.ReservedAt, nameof(command.ReservedAt));
        }

        if (!ModelState.IsValid)
        {
            return View(command);
        }

        reservation.CustomerName = command.CustomerName;
        reservation.PhoneNumber = command.PhoneNumber;
        reservation.PartySize = command.PartySize;
        reservation.ReservedAt = command.ReservedAt;
        await _db.SaveChangesAsync();
        TempData["Success"] = "已更新訂位。";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>直接刪除不留紀錄：訂位含姓名與電話，隱私權政策承諾顧客可請求刪除個資。</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var reservation = await _db.Reservations.FindAsync(id);
        if (reservation is null)
        {
            return NotFound();
        }

        _db.Reservations.Remove(reservation);
        await _db.SaveChangesAsync();
        TempData["Success"] = "已刪除訂位。";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateReservedAt(DateTime reservedAt, string fieldName)
    {
        // ReservedAt 為使用者輸入的台灣當地時間（非 UTC），故以台灣時間比對
        var now = TaipeiTime.Now;
        if (reservedAt < now)
        {
            ModelState.AddModelError(fieldName, "訂位時間不可早於現在");
        }
        else if (reservedAt > now.AddYears(1))
        {
            ModelState.AddModelError(fieldName, "訂位時間不可超過一年後");
        }
    }
}
