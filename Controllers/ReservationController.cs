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

    public async Task<IActionResult> Index(string? customerName, DateTime? date)
    {
        var query = _db.Reservations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(customerName))
        {
            query = query.Where(r => r.CustomerName.Contains(customerName));
        }

        if (date.HasValue)
        {
            var start = date.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(r => r.ReservedAt >= start && r.ReservedAt < end);
        }

        var results = await query.OrderBy(r => r.ReservedAt).ToListAsync();

        return View(new ReservationSearchViewModel
        {
            CustomerName = customerName,
            Date = date,
            Results = results
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
        // ReservedAt 為使用者輸入的台灣當地時間（非 UTC），故以台灣時間比對
        var now = TaipeiTime.Now;
        if (command.ReservedAt < now)
        {
            ModelState.AddModelError(nameof(command.ReservedAt), "訂位時間不可早於現在");
        }
        else if (command.ReservedAt > now.AddYears(1))
        {
            ModelState.AddModelError(nameof(command.ReservedAt), "訂位時間不可超過一年後");
        }

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
}
