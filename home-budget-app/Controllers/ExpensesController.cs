using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using home_budget_app.Data;
using home_budget_app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace home_budget_app.Controllers
{
    [Authorize]
    public class ExpensesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExpensesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // READ: Lista z paginacją, sortowaniem i filtrami (rok, miesiąc, kategoria)
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            string sortOrder = "date_desc",
            string categoryFilter = "",
            int? yearFilter = null,
            int? monthFilter = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 1) Base query
            var baseQuery = _context.Expenses.Where(e => e.UserId == userId);

            // 2) Filtracja rok/miesiąc
            if (yearFilter.HasValue)
                baseQuery = baseQuery.Where(e => e.Date.Year == yearFilter.Value);
            if (monthFilter.HasValue)
                baseQuery = baseQuery.Where(e => e.Date.Month == monthFilter.Value);

            // 3) Filtracja kategoria
            if (!string.IsNullOrEmpty(categoryFilter)
                && Enum.TryParse<ExpenseCategory>(categoryFilter, out var catEnum))
            {
                baseQuery = baseQuery.Where(e => e.Category == catEnum);
            }

            List<Expense> items;
            int totalItems;

            // 4) Sortowanie i paginacja
            switch (sortOrder.ToLower())
            {
                case "amount_asc":
                case "amount_desc":
                    var all = await baseQuery.ToListAsync();
                    var sortedByAmount = sortOrder == "amount_asc"
                        ? all.OrderBy(e => e.Amount)
                        : all.OrderByDescending(e => e.Amount);
                    totalItems = sortedByAmount.Count();
                    items = sortedByAmount
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                    break;
                case "date_asc":
                    baseQuery = baseQuery.OrderBy(e => e.Date);
                    goto Sql;
                default:
                    baseQuery = baseQuery.OrderByDescending(e => e.Date);
                    goto Sql;
            }

        Sql:
            totalItems = await baseQuery.CountAsync();
            items = await baseQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ViewBag dla widoku
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.SortOrder = sortOrder;
            ViewBag.CategoryFilter = categoryFilter;
            ViewBag.YearFilter = yearFilter;
            ViewBag.MonthFilter = monthFilter;

            // Lista lat: zawsze zawiera bieżący rok i lata z bazy
            var years = await _context.Expenses
                .Where(e => e.UserId == userId)
                .Select(e => e.Date.Year)
                .Distinct()
                .ToListAsync();
            if (!years.Contains(DateTime.Now.Year))
                years.Add(DateTime.Now.Year);
            ViewBag.Years = years.OrderByDescending(y => y).ToList();

            // Lista miesięcy: zawsze 1-12
            ViewBag.Months = Enumerable.Range(1, 12)
                .Select(m => new
                {
                    Number = m,
                    Name = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)
                })
                .ToList();

            return View(items);
        }

        // READ: Szczegóły
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var expense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (expense == null) return NotFound();
            return View(expense);
        }

        // CREATE GET
        public IActionResult Create()
        {
            ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory));
            return View();
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense)
        {
            if (ModelState.IsValid)
            {
                expense.UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
                _context.Add(expense);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory));
            return View(expense);
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null || expense.UserId != User.FindFirst(ClaimTypes.NameIdentifier).Value)
                return NotFound();
            ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory));
            return View(expense);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Expense expense)
        {
            if (id != expense.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    expense.UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
                    _context.Update(expense);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExpenseExists(expense.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory));
            return View(expense);
        }

        // DELETE GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var expense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (expense == null) return NotFound();
            return View(expense);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense != null && expense.UserId == User.FindFirst(ClaimTypes.NameIdentifier).Value)
            {
                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ExpenseExists(int id)
        {
            return _context.Expenses.Any(e => e.Id == id && e.UserId == User.FindFirst(ClaimTypes.NameIdentifier).Value);
        }
    }
}
