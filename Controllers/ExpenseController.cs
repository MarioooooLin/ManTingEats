using ManTingEats.Data;
using ManTingEats.Models;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using ManTingEats.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManTingEats.Controllers;

// 店長限定（v7）：員工只能使用訂單與訂位
[Authorize(Roles = AppRoles.Manager)]
public sealed class ExpenseController : Controller
{
    private readonly AppDbContext _db;

    public ExpenseController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(DateTime? start, DateTime? end, ExpenseCategory? category, int page = 1)
    {
        var query = _db.Expenses.AsQueryable();

        if (start.HasValue)
        {
            query = query.Where(e => e.Date >= start.Value.Date);
        }

        if (end.HasValue)
        {
            var endExclusive = end.Value.Date.AddDays(1);
            query = query.Where(e => e.Date < endExclusive);
        }

        if (category.HasValue)
        {
            query = query.Where(e => e.Category == category.Value);
        }

        // 支出總額需涵蓋所有符合條件的資料，不能只加總目前這一頁
        var totalAmount = await query.SumAsync(e => e.Amount);
        var results = await PagedList<Expense>.CreateAsync(query.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id), page);

        return View(new ExpenseSearchViewModel
        {
            Start = start,
            End = end,
            Category = category,
            Results = results,
            TotalAmount = totalAmount
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        // 支出以營業日記帳（v12）：打烊後凌晨記帳仍帶入當晚的營業日，報表才能與同晚的營收對上
        return View(new CreateExpenseCommand(TaipeiTime.BusinessToday, ExpenseCategory.Ingredients, 0, null));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateExpenseCommand command)
    {
        ValidateExpenseDate(command.Date, nameof(command.Date));
        if (!ModelState.IsValid)
        {
            return View(command);
        }

        _db.Expenses.Add(new Expense
        {
            Date = command.Date,
            Category = command.Category,
            Amount = command.Amount,
            Note = command.Note
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "已新增支出記錄。";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense is null)
        {
            return NotFound();
        }

        return View(new UpdateExpenseCommand(expense.Id, expense.Date, expense.Category, expense.Amount, expense.Note));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateExpenseCommand command)
    {
        ValidateExpenseDate(command.Date, nameof(command.Date));
        if (!ModelState.IsValid)
        {
            return View(command);
        }

        var expense = await _db.Expenses.FindAsync(command.Id);
        if (expense is null)
        {
            return NotFound();
        }

        expense.Date = command.Date;
        expense.Category = command.Category;
        expense.Amount = command.Amount;
        expense.Note = command.Note;
        await _db.SaveChangesAsync();
        TempData["Success"] = "已更新支出記錄。";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>支出僅登記已發生的費用，營業日不可晚於目前營業日（v12，06:00 切換）；補登過去日期不受限。</summary>
    private void ValidateExpenseDate(DateTime date, string fieldName)
    {
        if (date.Date > TaipeiTime.BusinessToday)
        {
            ModelState.AddModelError(fieldName, "營業日不可晚於目前營業日");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense is null)
        {
            return NotFound();
        }

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();
        TempData["Success"] = "已刪除支出記錄。";
        return RedirectToAction(nameof(Index));
    }
}
