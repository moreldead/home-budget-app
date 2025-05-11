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

            // Jeśli year i month są null, przypisz bieżący rok i miesiąc
            year ??= DateTime.Now.Year;
            month ??= DateTime.Now.Month;

            // Przygotowanie nazw miesięcy w kontrolerze
            var monthNames = Enumerable.Range(1, 12)
                .Select(month => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month))
                .ToArray();

            // Przekazanie do widoku
            ViewBag.MonthNames = monthNames;
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

            ViewBag.ExpensesSum = await expenseQuery.SumAsync(e => (double)e.Amount);
            ViewBag.IncomesSum = await incomeQuery.SumAsync(i => (double)i.Amount);

            // Pobranie sumy wydatków z całego wybranego roku
            ViewBag.TotalExpensesYear = await _context.Expenses
                .Where(e => e.UserId == userId && e.Date.Year == year)
                .SumAsync(e => (double)e.Amount);

            // Inicjalizacja słowników dla miesięcznych sum wydatków i dochodów
            Dictionary<int, double> monthlyExpensesSums = new Dictionary<int, double>();
            Dictionary<int, double> monthlyExpensesSumsChart = new Dictionary<int, double>();
            Dictionary<int, double> monthlyIncomesSums = new Dictionary<int, double>();

            // Iteracja przez wszystkie miesiące w roku
            for (int monthIter = 1; monthIter <= 12; monthIter++)
            {
                // Suma wydatków za dany miesiąc
                double monthlyExpenseSum = await _context.Expenses
                    .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == monthIter)
                    .SumAsync(e => (double)e.Amount) * -1;

                // Suma dochodów za dany miesiąc
                double monthlyIncomeSum = await _context.Incomes
                    .Where(i => i.UserId == userId && i.Date.Year == year && i.Date.Month == monthIter)
                    .SumAsync(i => (double)i.Amount);

                // Dodajemy sumy do słowników
                monthlyExpensesSums[monthIter] = monthlyExpenseSum;
                monthlyExpensesSumsChart[monthIter] = monthlyExpenseSum;
                monthlyIncomesSums[monthIter] = monthlyIncomeSum;
            }

            // Przekazanie słowników z sumami wydatków i dochodów do ViewBag
            ViewBag.MonthlyExpensesSums = monthlyExpensesSums;
            ViewBag.MonthlyExpensesSumsChart = monthlyExpensesSumsChart;
            ViewBag.MonthlyIncomesSums = monthlyIncomesSums;

            // Wyświetlanie sum wydatków dla każdej kategorii
            Dictionary<string, double> expensesSums = new Dictionary<string, double>();

            // Iterujemy przez wszystkie kategorie w ExpenseCategory
            foreach (var category in Enum.GetValues(typeof(ExpenseCategory)).Cast<ExpenseCategory>())
            {
                // Obliczamy sumę wydatków dla danej kategorii
                double categorySum = await _context.Expenses
                    .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == month && e.Category == category)
                    .SumAsync(e => (double)e.Amount);

                // Przechowujemy sumę w słowniku z nazwą kategorii
                expensesSums[category.ToString()] = categorySum;
            }

            // Przekazujemy słownik do ViewBag
            ViewBag.ExpensesSums = expensesSums;


            // Wyświetlanie sum wydatków w danym roku według kategorii
            Dictionary<string, double> expensesSumsByCategory = new Dictionary<string, double>();

            // Iterujemy przez wszystkie kategorie w ExpenseCategory
            foreach (var category in Enum.GetValues(typeof(ExpenseCategory)).Cast<ExpenseCategory>())
            {
                // Obliczamy sumę wydatków dla danej kategorii w wybranym roku
                double categorySum = await _context.Expenses
                    .Where(e => e.UserId == userId && e.Date.Year == year && e.Category == category)
                    .SumAsync(e => (double)e.Amount);

                // Dodajemy sumę do słownika z nazwą kategorii
                expensesSumsByCategory[category.GetDisplayName()] = categorySum; // Używamy GetDisplayName() do wyświetlania przyjaznej nazwy
            }

            // Przekazujemy słownik sum wydatków według kategorii do ViewBag
            ViewBag.ExpensesSumsByCategory = expensesSumsByCategory;


            {  // Wyświetlanie sum wydatków w wybranym miesiącu według kategorii
                Dictionary<string, double> expensesSumsByCategoryForMonth = new Dictionary<string, double>();

                // Iterujemy przez wszystkie kategorie w ExpenseCategory
                foreach (var category in Enum.GetValues(typeof(ExpenseCategory)).Cast<ExpenseCategory>())
                {
                    // Obliczamy sumę wydatków dla danej kategorii w wybranym miesiącu
                    double categorySum = await _context.Expenses
                        .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == month && e.Category == category)
                        .SumAsync(e => (double)e.Amount);

                    // Dodajemy sumę do słownika z nazwą kategorii
                    expensesSumsByCategoryForMonth[category.GetDisplayName()] = categorySum; // Używamy GetDisplayName() do wyświetlania przyjaznej nazwy
                }

                // Przekazujemy słownik sum wydatków według kategorii w wybranym miesiącu do ViewBag
                ViewBag.ExpensesSumsByCategoryForMonth = expensesSumsByCategoryForMonth;
            }


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
