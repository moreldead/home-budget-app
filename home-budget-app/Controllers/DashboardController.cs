// Plik: home-budget-app/Controllers/DashboardController.cs
using home_budget_app.Data;
using home_budget_app.Extensions; //
using home_budget_app.Models; //
using Microsoft.AspNetCore.Authorization; //
using Microsoft.AspNetCore.Mvc; //
using Microsoft.EntityFrameworkCore; //
using System; // Dodano dla DateTime
using System.Collections.Generic; // Dodano dla List i Dictionary
using System.Globalization; //
using System.Linq; //
using System.Security.Claims; // Dodano dla ClaimTypes
using System.Threading.Tasks; //

namespace home_budget_app.Controllers
{
    [Authorize] //
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context; //

        public DashboardController(ApplicationDbContext context) //
        {
            _context = context; //
        }

        // Dodajemy sortOrder do parametrów, podobnie jak w Incomes/Expenses
        public async Task<IActionResult> Index(int? year, int? month, string sortOrder = "date_desc") //
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value; //

            year ??= DateTime.Now.Year; //
            month ??= DateTime.Now.Month; //

            ViewBag.SelectedYear = year; //
            ViewBag.SelectedMonth = month; //
            ViewBag.SortOrder = sortOrder; // Przekazujemy sortOrder do widoku

            // ... (reszta kodu do pobierania sum, lat, miesięcy, danych do wykresów - bez zmian) ...
            var monthNames = Enumerable.Range(1, 12)
                .Select(mVal => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(mVal))
                .ToArray();
            ViewBag.MonthNames = monthNames; //

            ViewBag.Years = await _context.Expenses
                .Where(e => e.UserId == userId)
                .Select(e => e.Date.Year)
                .Union(_context.Incomes.Where(i => i.UserId == userId).Select(i => i.Date.Year))
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync(); //

            ViewBag.Months = Enumerable.Range(1, 12).Select(i => new
            {
                Number = i,
                Name = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)
            }).ToList(); //

            var expenseQuery = _context.Expenses
                .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == month); //

            var incomeQuery = _context.Incomes
                .Where(i => i.UserId == userId && i.Date.Year == year && i.Date.Month == month); //

            ViewBag.ExpensesSum = await expenseQuery.SumAsync(e => (double)e.Amount); //
            ViewBag.IncomesSum = await incomeQuery.SumAsync(i => (double)i.Amount); //

            ViewBag.TotalExpensesYear = await _context.Expenses
                .Where(e => e.UserId == userId && e.Date.Year == year)
                .SumAsync(e => (double)e.Amount); //

            Dictionary<int, double> monthlyExpensesSumsChart = new Dictionary<int, double>(); //
            Dictionary<int, double> monthlyIncomesSums = new Dictionary<int, double>(); //
            for (int monthIter = 1; monthIter <= 12; monthIter++) //
            {
                // W oryginalnym kodzie monthlyExpenseSum było mnożone przez -1, tutaj tego nie robimy dla spójności danych do wykresu
                monthlyExpensesSumsChart[monthIter] = await _context.Expenses
                    .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == monthIter)
                    .SumAsync(e => (double)e.Amount); //

                monthlyIncomesSums[monthIter] = await _context.Incomes
                    .Where(i => i.UserId == userId && i.Date.Year == year && i.Date.Month == monthIter)
                    .SumAsync(i => (double)i.Amount); //
            }
            ViewBag.MonthlyExpensesSumsChart = monthlyExpensesSumsChart; //
            ViewBag.MonthlyIncomesSums = monthlyIncomesSums; //

            // Logika dla tabeli rocznej sum wydatków/dochodów (ViewBag.MonthlyExpensesSums używany w tabeli rocznej)
            Dictionary<int, double> monthlyDisplayExpensesSums = new Dictionary<int, double>();
            for (int monthIter = 1; monthIter <= 12; monthIter++)
            {
                monthlyDisplayExpensesSums[monthIter] = await _context.Expenses
                    .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == monthIter)
                    .SumAsync(e => (double)e.Amount); // Tutaj może być potrzebne -1 jeśli tak ma być w tabeli
            }
            ViewBag.MonthlyExpensesSums = monthlyDisplayExpensesSums; // Nadpisujemy dla tabeli rocznej jeśli potrzebna inna logika niż dla wykresu

            Dictionary<string, double> expensesSumsByCategory = new Dictionary<string, double>(); //
            foreach (var category in Enum.GetValues(typeof(ExpenseCategory)).Cast<ExpenseCategory>()) //
            {
                double categorySum = await _context.Expenses
                    .Where(e => e.UserId == userId && e.Date.Year == year && e.Category == category)
                    .SumAsync(e => (double)e.Amount); //
                expensesSumsByCategory[category.GetDisplayName()] = categorySum; //
            }
            ViewBag.ExpensesSumsByCategory = expensesSumsByCategory; //

            Dictionary<string, double> expensesSumsByCategoryForMonth = new Dictionary<string, double>(); //
            foreach (var category in Enum.GetValues(typeof(ExpenseCategory)).Cast<ExpenseCategory>()) //
            {
                double categorySum = await _context.Expenses
                    .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == month && e.Category == category)
                    .SumAsync(e => (double)e.Amount); //
                expensesSumsByCategoryForMonth[category.GetDisplayName()] = categorySum; //
            }
            ViewBag.ExpensesSumsByCategoryForMonth = expensesSumsByCategoryForMonth; //


            // Pobranie listy wydatków i dochodów w danym miesiącu z dodaniem ID
            var expensesForTable = await _context.Expenses
                .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == month)
                .Select(e => new TransactionViewModel // Używamy ViewModelu
                {
                    Id = e.Id, // Dodajemy Id
                    Type = "Wydatek", //
                    Date = e.Date, //
                    Category = e.Category.GetDisplayName(), //
                    Amount = e.Amount, //
                    Notes = e.Notes //
                })
                .ToListAsync(); //

            var incomesForTable = await _context.Incomes
                .Where(i => i.UserId == userId && i.Date.Year == year && i.Date.Month == month)
                .Select(i => new TransactionViewModel // Używamy ViewModelu
                {
                    Id = i.Id, // Dodajemy Id
                    Type = "Dochód", //
                    Date = i.Date, //
                    Category = i.Category.GetDisplayName(), //
                    Amount = i.Amount, //
                    Notes = i.Notes //
                })
                .ToListAsync(); //

            IEnumerable<TransactionViewModel> recentEntries = expensesForTable.Concat(incomesForTable); //

            // Logika sortowania dla recentEntries
            sortOrder = string.IsNullOrWhiteSpace(sortOrder) ? "date_desc" : sortOrder.ToLower();
            // ViewBag.SortOrder już ustawiony na początku metody

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
                case "date_desc": // domyślne
                default:
                    recentEntries = recentEntries.OrderByDescending(e => e.Date);
                    ViewBag.SortOrder = "date_desc"; // Upewnij się, że to jest ustawione dla domyślnego
                    break;
            }

            ViewBag.RecentEntries = recentEntries.ToList(); //

            return View(); //
        }
    }

    // Prosty ViewModel do reprezentowania transakcji w tabeli RecentEntries
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