using ManTingEats.Data;
using ManTingEats.Models;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManTingEats.Controllers;

[Authorize]
public sealed class ExpenseController : Controller
{
    private readonly AppDbContext _db;

    public ExpenseController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(DateTime? start, DateTime? end, ExpenseCategory? category)
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

        var results = await query.OrderByDescending(e => e.Date).ToListAsync();

        return View(new ExpenseSearchViewModel
        {
            Start = start,
            End = end,
            Category = category,
            Results = results,
            TotalAmount = results.Sum(e => e.Amount)
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateExpenseCommand(DateTime.Today, ExpenseCategory.Ingredients, 0, null));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateExpenseCommand command)
    {
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
