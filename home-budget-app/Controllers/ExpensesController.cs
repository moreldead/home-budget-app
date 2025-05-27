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
            _context = context ?? throw new ArgumentNullException(nameof(context)); // dodano sprawdzenie nulla dla contextu, tak jak w incomescontroller
        }
        // Plik: home-budget-app/Controllers/ExpensesController.cs

// ... (istniejące usingi i deklaracja klasy) ...

        // get: wydatki - lista z paginacją, sortowaniem i filtrami (rok, miesiąc, kategoria)
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            string sortOrder = "date_desc",
            string categoryFilter = "",
            int? yearFilter = null,
            int? monthFilter = null)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge(); //
            }

            IQueryable<Expense> baseQuery = _context.Expenses.Where(e => e.UserId == userId); //

            if (yearFilter.HasValue)
            {
                baseQuery = baseQuery.Where(e => e.Date.Year == yearFilter.Value); //
            }
            if (monthFilter.HasValue)
            {
                baseQuery = baseQuery.Where(e => e.Date.Month == monthFilter.Value); //
            }
            if (!string.IsNullOrEmpty(categoryFilter) && Enum.TryParse<ExpenseCategory>(categoryFilter, out var catEnum))
            {
                baseQuery = baseQuery.Where(e => e.Category == catEnum); //
            }

            sortOrder = string.IsNullOrWhiteSpace(sortOrder) ? "date_desc" : sortOrder.ToLower();
            ViewBag.SortOrder = sortOrder; //

            IQueryable<Expense> queryableItems = baseQuery; //
            List<Expense> items; //
            int totalItems; //

            switch (sortOrder)
            {
                // DODANE SORTOWANIE PO ID
                case "id_asc":
                    queryableItems = queryableItems.OrderBy(e => e.Id);
                    break;
                case "id_desc":
                    queryableItems = queryableItems.OrderByDescending(e => e.Id);
                    break;
                // POPRAWIONE I DODANE SORTOWANIE PO KATEGORII
                case "category_asc":
                    queryableItems = queryableItems.OrderBy(e => e.Category).ThenByDescending(e => e.Date); //
                    break;
                case "category_desc":
                    queryableItems = queryableItems.OrderByDescending(e => e.Category).ThenByDescending(e => e.Date); //
                    break;
                case "date_asc":
                    queryableItems = queryableItems.OrderBy(e => e.Date); //
                    break;
                // "amount_asc" i "amount_desc" są obsługiwane po switchu
                case "date_desc":
                default:
                    queryableItems = queryableItems.OrderByDescending(e => e.Date); //
                    // Poprawiony warunek dla domyślnego sortOrder w ViewBag
                    if (sortOrder != "amount_asc" && sortOrder != "amount_desc" &&
                        sortOrder != "id_asc" && sortOrder != "id_desc" &&
                        sortOrder != "category_asc" && sortOrder != "category_desc")
                    {
                        ViewBag.SortOrder = "date_desc"; //
                    }
                    break;
            }

            totalItems = await queryableItems.CountAsync(); //

            if (sortOrder == "amount_asc" || sortOrder == "amount_desc")
            {
                var allItemsForAmountSort = await queryableItems.ToListAsync(); //
                if (sortOrder == "amount_asc")
                {
                    items = allItemsForAmountSort.OrderBy(e => e.Amount).Skip((page - 1) * pageSize).Take(pageSize).ToList(); //
                }
                else // amount_desc
                {
                    items = allItemsForAmountSort.OrderByDescending(e => e.Amount).Skip((page - 1) * pageSize).Take(pageSize).ToList(); //
                }
            }
            else
            {
                items = await queryableItems.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(); //
            }

            ViewBag.CurrentPage = page; //
            ViewBag.PageSize = pageSize; //
            ViewBag.TotalItems = totalItems; //
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize); //
            ViewBag.CategoryFilter = categoryFilter; //
            ViewBag.YearFilter = yearFilter; //
            ViewBag.MonthFilter = monthFilter; //

            var years = await _context.Expenses.Where(e => e.UserId == userId).Select(e => e.Date.Year).Distinct().ToListAsync(); //
            if (!years.Contains(DateTime.Now.Year)) //
            {
                years.Add(DateTime.Now.Year); //
            }
            ViewBag.Years = years.OrderByDescending(y => y).ToList(); //

            ViewBag.Months = Enumerable.Range(1, 12).Select(m => new { Number = m, Name = CultureInfo.CurrentCulture?.DateTimeFormat?.GetMonthName(m) ?? m.ToString() }).ToList(); //
            
            return View(items); //
        }

        // ... (reszta metod kontrolera: Details, Create, Edit, Delete, ExpenseExists) ...
        // Metody Details, Create (GET/POST), Edit (GET/POST), Delete (GET/POST) oraz ExpenseExists
        // pozostają takie same jak w dostarczonym przez Ciebie kodzie ExpensesController.cs,
        // ponieważ zmiany dotyczą głównie metody Index.
        // Poniżej skrócona wersja dla kompletności, ale załóż, że są one takie jak w twoim oryginalnym pliku.

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound(); //
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //
            if (string.IsNullOrEmpty(userId)) return Challenge(); //
            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId); //
            if (expense == null) return NotFound(); //
            return View(expense); //
        }

        public IActionResult Create()
        {
            var model = new Expense { Date = DateTime.Today }; //
            ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory)); //
            return View(model); //
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Category,Date,Amount,Notes")] Expense expense) //
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError(string.Empty, "użytkownik nie jest poprawnie zidentyfikowany. spróbuj zalogować się ponownie."); //
                ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory)); //
                return View(expense); //
            }
            expense.UserId = userId; //
            if (ModelState.IsValid) //
            {
                _context.Add(expense); //
                await _context.SaveChangesAsync(); //
                return RedirectToAction(nameof(Index)); //
            }
            ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory)); //
            return View(expense); //
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound(); //
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //
            if (string.IsNullOrEmpty(userId)) return Challenge(); //
            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId); //
            if (expense == null) return NotFound(); //
            ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory)); //
            return View(expense); //
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Category,Date,Amount,Notes")] Expense expense) //
        {
            if (id != expense.Id) return NotFound(); //
            string? currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //
            if (string.IsNullOrEmpty(currentUserId))
            {
                ModelState.AddModelError(string.Empty, "nie można zweryfikować użytkownika. spróbuj zalogować się ponownie."); //
                ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory)); //
                return View(expense); //
            }
            var expenseToUpdate = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == currentUserId); //
            if (expenseToUpdate == null) return NotFound(); //

            expenseToUpdate.Category = expense.Category; //
            expenseToUpdate.Date = expense.Date; //
            expenseToUpdate.Amount = expense.Amount; //
            expenseToUpdate.Notes = expense.Notes; //

            if (ModelState.IsValid) //
            {
                try
                {
                    await _context.SaveChangesAsync(); //
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExpenseExists(expenseToUpdate.Id, currentUserId)) return NotFound(); //
                    else throw; //
                }
                return RedirectToAction(nameof(Index)); //
            }
            ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory)); //
            return View(expenseToUpdate); //
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound(); //
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //
            if (string.IsNullOrEmpty(userId)) return Challenge(); //
            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId); //
            if (expense == null) return NotFound(); //
            return View(expense); //
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id) //
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //
            if (string.IsNullOrEmpty(userId)) return Challenge(); //
            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId); //
            if (expense != null) //
            {
                _context.Expenses.Remove(expense); //
                await _context.SaveChangesAsync(); //
                TempData["SuccessMessage"] = "wydatek został pomyślnie usunięty."; //
            }
            else
            {
                TempData["ErrorMessage"] = "nie można usunąć wydatku. nie został znaleziony lub nie masz uprawnień."; //
            }
            return RedirectToAction(nameof(Index)); //
        }

        private bool ExpenseExists(int id, string userId) //
        {
            return _context.Expenses.Any(e => e.Id == id && e.UserId == userId); //
        }
    }
}