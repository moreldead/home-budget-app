// Plik: home-budget-app/Controllers/DashboardController.cs
using home_budget_app.Data;
using home_budget_app.Extensions;
using home_budget_app.Models; // Make sure this using statement is present for MonthlySummaryViewModel
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace home_budget_app.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Signature updated to include summarySortOrderParam
        public async Task<IActionResult> Index(int? year, int? month, string sortOrder = "date_desc", string summarySortOrderParam = "summary_month_asc")
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            year ??= DateTime.Now.Year;
            month ??= DateTime.Now.Month;

            ViewBag.SelectedYear = year;
            ViewBag.SelectedMonth = month;
            ViewBag.SortOrder = sortOrder; // For the main transaction table

            var monthNames = Enumerable.Range(1, 12)
                .Select(mVal => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(mVal))
                .ToArray();
            ViewBag.MonthNames = monthNames;

            ViewBag.Years = await _context.Expenses
                .Where(e => e.UserId == userId)
                .Select(e => e.Date.Year)
                .Union(_context.Incomes.Where(i => i.UserId == userId).Select(i => i.Date.Year))
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            ViewBag.Months = Enumerable.Range(1, 12).Select(i => new
            {
                Number = i,
                Name = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)
            }).ToList();

            var expenseQuery = _context.Expenses
                .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == month);

            var incomeQuery = _context.Incomes
                .Where(i => i.UserId == userId && i.Date.Year == year && i.Date.Month == month);

            ViewBag.ExpensesSum = await expenseQuery.SumAsync(e => (double)e.Amount);
            ViewBag.IncomesSum = await incomeQuery.SumAsync(i => (double)i.Amount);

            ViewBag.TotalExpensesYear = await _context.Expenses
                .Where(e => e.UserId == userId && e.Date.Year == year)
                .SumAsync(e => (double)e.Amount);

            Dictionary<int, double> monthlyExpensesSumsChart = new Dictionary<int, double>();
            Dictionary<int, double> monthlyIncomesSums = new Dictionary<int, double>();
            for (int monthIter = 1; monthIter <= 12; monthIter++)
            {
                monthlyExpensesSumsChart[monthIter] = await _context.Expenses
                    .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == monthIter)
                    .SumAsync(e => (double)e.Amount);

                monthlyIncomesSums[monthIter] = await _context.Incomes
                    .Where(i => i.UserId == userId && i.Date.Year == year && i.Date.Month == monthIter)
                    .SumAsync(i => (double)i.Amount);
            }
            ViewBag.MonthlyExpensesSumsChart = monthlyExpensesSumsChart;
            ViewBag.MonthlyIncomesSums = monthlyIncomesSums; // Used for both chart and summary table

            Dictionary<int, double> monthlyDisplayExpensesSums = new Dictionary<int, double>();
            for (int monthIter = 1; monthIter <= 12; monthIter++)
            {
                monthlyDisplayExpensesSums[monthIter] = await _context.Expenses
                    .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == monthIter)
                    .SumAsync(e => (double)e.Amount);
            }
            ViewBag.MonthlyExpensesSums = monthlyDisplayExpensesSums; // This is specifically for the summary table if different logic needed than chart

            // --- New logic for the Yearly Summary Table (Sumy wydatków i dochodów za rok) ---
            var monthlySummaries = new List<MonthlySummaryViewModel>();
            // Use the ViewBag properties that are already populated for the table
            var currentMonthlyExpenses = ViewBag.MonthlyExpensesSums as Dictionary<int, double> ?? new Dictionary<int, double>();
            var currentMonthlyIncomes = ViewBag.MonthlyIncomesSums as Dictionary<int, double> ?? new Dictionary<int, double>();

            for (int m = 1; m <= 12; m++)
            {
                monthlySummaries.Add(new MonthlySummaryViewModel
                {
                    MonthNumber = m,
                    MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m), // Or your preferred culture
                    TotalExpenses = currentMonthlyExpenses.TryGetValue(m, out var exp) ? exp : 0,
                    TotalIncomes = currentMonthlyIncomes.TryGetValue(m, out var inc) ? inc : 0
                });
            }

            ViewBag.CurrentSummarySort = summarySortOrderParam;

            switch (summarySortOrderParam.ToLower()) // Ensure case-insensitivity
            {
                case "summary_month_desc":
                    monthlySummaries = monthlySummaries.OrderByDescending(s => s.MonthNumber).ToList();
                    break;
                case "summary_expenses_asc":
                    monthlySummaries = monthlySummaries.OrderBy(s => s.TotalExpenses).ToList();
                    break;
                case "summary_expenses_desc":
                    monthlySummaries = monthlySummaries.OrderByDescending(s => s.TotalExpenses).ToList();
                    break;
                case "summary_incomes_asc":
                    monthlySummaries = monthlySummaries.OrderBy(s => s.TotalIncomes).ToList();
                    break;
                case "summary_incomes_desc":
                    monthlySummaries = monthlySummaries.OrderByDescending(s => s.TotalIncomes).ToList();
                    break;
                case "summary_month_asc":
                default:
                    monthlySummaries = monthlySummaries.OrderBy(s => s.MonthNumber).ToList();
                    // Ensure default is reflected in ViewBag if summarySortOrderParam was null/empty or didn't match
                    if (string.IsNullOrEmpty(summarySortOrderParam) || !summarySortOrderParam.ToLower().StartsWith("summary_"))
                    {
                        ViewBag.CurrentSummarySort = "summary_month_asc";
                    }
                    break;
            }
            ViewBag.MonthlySummaries = monthlySummaries;
            // --- End of New logic for Yearly Summary Table ---

            Dictionary<string, double> expensesSumsByCategory = new Dictionary<string, double>();
            foreach (var category in Enum.GetValues(typeof(ExpenseCategory)).Cast<ExpenseCategory>())
            {
                double categorySum = await _context.Expenses
                    .Where(e => e.UserId == userId && e.Date.Year == year && e.Category == category)
                    .SumAsync(e => (double)e.Amount);
                expensesSumsByCategory[category.GetDisplayName()] = categorySum;
            }
            ViewBag.ExpensesSumsByCategory = expensesSumsByCategory;

            Dictionary<string, double> expensesSumsByCategoryForMonth = new Dictionary<string, double>();
            foreach (var category in Enum.GetValues(typeof(ExpenseCategory)).Cast<ExpenseCategory>())
            {
                double categorySum = await _context.Expenses
                    .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == month && e.Category == category)
                    .SumAsync(e => (double)e.Amount);
                expensesSumsByCategoryForMonth[category.GetDisplayName()] = categorySum;
            }
            ViewBag.ExpensesSumsByCategoryForMonth = expensesSumsByCategoryForMonth;

            var expensesForTable = await _context.Expenses
                .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == month)
                .Select(e => new TransactionViewModel
                {
                    Id = e.Id,
                    Type = "Wydatek",
                    Date = e.Date,
                    Category = e.Category.GetDisplayName(),
                    Amount = e.Amount,
                    Notes = e.Notes
                })
                .ToListAsync();

            var incomesForTable = await _context.Incomes
                .Where(i => i.UserId == userId && i.Date.Year == year && i.Date.Month == month)
                .Select(i => new TransactionViewModel
                {
                    Id = i.Id,
                    Type = "Dochód",
                    Date = i.Date,
                    Category = i.Category.GetDisplayName(),
                    Amount = i.Amount,
                    Notes = i.Notes
                })
                .ToListAsync();

            IEnumerable<TransactionViewModel> recentEntries = expensesForTable.Concat(incomesForTable);

            sortOrder = string.IsNullOrWhiteSpace(sortOrder) ? "date_desc" : sortOrder.ToLower();

            switch (sortOrder)
            {
                case "id_asc":
                    recentEntries = recentEntries.OrderBy(e => e.Id);
                    break;
                case "id_desc":
                    recentEntries = recentEntries.OrderByDescending(e => e.Id);
                    break;
                case "type_asc":
                    recentEntries = recentEntries.OrderBy(e => e.Type).ThenByDescending(e => e.Date);
                    break;
                case "type_desc":
                    recentEntries = recentEntries.OrderByDescending(e => e.Type).ThenByDescending(e => e.Date);
                    break;
                case "category_asc":
                    recentEntries = recentEntries.OrderBy(e => e.Category).ThenByDescending(e => e.Date);
                    break;
                case "category_desc":
                    recentEntries = recentEntries.OrderByDescending(e => e.Category).ThenByDescending(e => e.Date);
                    break;
                case "amount_asc":
                    recentEntries = recentEntries.OrderBy(e => e.Amount);
                    break;
                case "amount_desc":
                    recentEntries = recentEntries.OrderByDescending(e => e.Amount);
                    break;
                case "date_asc":
                    recentEntries = recentEntries.OrderBy(e => e.Date);
                    break;
                case "date_desc":
                default:
                    recentEntries = recentEntries.OrderByDescending(e => e.Date);
                    ViewBag.SortOrder = "date_desc";
                    break;
            }

            ViewBag.RecentEntries = recentEntries.ToList();

            return View();
        }
    }

    // This ViewModel is defined inline in your original code.
    // It's generally better practice to have ViewModels in separate files in a Models or ViewModels folder.
    public class TransactionViewModel
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public string Notes { get; set; }
    }
}