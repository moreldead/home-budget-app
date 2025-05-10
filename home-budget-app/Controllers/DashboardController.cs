using home_budget_app.Data;
using home_budget_app.Extensions;
using home_budget_app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
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

        public async Task<IActionResult> Index(int? year, int? month)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var currentDate = DateTime.Now;
            year ??= currentDate.Year;
            month ??= currentDate.Month;

            // Przekazanie wybranego roku i miesiąca do widoku
            ViewBag.SelectedYear = year;
            ViewBag.SelectedMonth = month;

            // Pobranie dostępnych lat dla użytkownika
            ViewBag.Years = await _context.Expenses
                .Where(e => e.UserId == userId)
                .Select(e => e.Date.Year)
                .Union(_context.Incomes.Where(i => i.UserId == userId).Select(i => i.Date.Year))
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            // Pobranie miesięcy do wyświetlenia
            ViewBag.Months = Enumerable.Range(1, 12).Select(i => new
            {
                Number = i,
                Name = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)
            }).ToList();

            // Pobranie sum wydatków i dochodów w danym miesiącu
            var expenseQuery = _context.Expenses
                .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == month);

            var incomeQuery = _context.Incomes
                .Where(i => i.UserId == userId && i.Date.Year == year && i.Date.Month == month);

            // Pobranie sumy wydatków
            ViewBag.ExpensesSum = await expenseQuery.SumAsync(e => (double)e.Amount);

            // Pobranie sumy dochodów
            ViewBag.IncomesSum = await incomeQuery.SumAsync(i => (double)i.Amount);

            // Pobranie sumy wydatków z kategorii Food
            ViewBag.FoodExpensesSum = await _context.Expenses
                .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == month && e.Category == ExpenseCategory.Food)
                .SumAsync(e => (double)e.Amount);

            // Pobranie średniej wydatków z kategorii Food w danym roku
            ViewBag.FoodAverageYear = await _context.Expenses
                .Where(e => e.UserId == userId && e.Date.Year == year && e.Category == ExpenseCategory.Food)
                .AverageAsync(e => (double)e.Amount);

            // Pobranie listy wszystkich wydatków i dochodów w danym miesiącu
            var expenses = await _context.Expenses
                .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == month)
                .OrderByDescending(e => e.Date)
                .Select(e => new
                {
                    Type = "Wydatek",
                    e.Date,
                    Category = e.Category.GetDisplayName(),
                    e.Amount,
                    e.Notes
                })
                .ToListAsync();

            var incomes = await _context.Incomes
                .Where(i => i.UserId == userId && i.Date.Year == year && i.Date.Month == month)
                .OrderByDescending(i => i.Date)
                .Select(i => new
                {
                    Type = "Dochód",
                    i.Date,
                    Category = i.Category.GetDisplayName(),
                    i.Amount,
                    i.Notes
                })
                .ToListAsync();

            // Połączenie wydatków i dochodów w jedną listę
            ViewBag.RecentEntries = expenses.Concat(incomes)
                .OrderByDescending(e => e.Date)
                .ToList();

            return View();
        }
    }
}
