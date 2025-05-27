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

        // get: wydatki - lista z paginacją, sortowaniem i filtrami (rok, miesiąc, kategoria)
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            string sortOrder = "date_desc",
            string categoryFilter = "",
            int? yearFilter = null,
            int? monthFilter = null)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // userid może być null
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge(); // jeśli userid jest pusty, użytkownik nie jest autoryzowany
            }

            // 1) bazowe zapytanie - pobieramy tylko wydatki zalogowanego użytkownika
            IQueryable<Expense> baseQuery = _context.Expenses.Where(e => e.UserId == userId); //

            // 2) filtrowanie po roku
            if (yearFilter.HasValue)
            {
                baseQuery = baseQuery.Where(e => e.Date.Year == yearFilter.Value); //
            }
            // 3) filtrowanie po miesiącu
            if (monthFilter.HasValue)
            {
                baseQuery = baseQuery.Where(e => e.Date.Month == monthFilter.Value); //
            }
            // 4) filtrowanie po kategorii
            if (!string.IsNullOrEmpty(categoryFilter) && Enum.TryParse<ExpenseCategory>(categoryFilter, out var catEnum))
            {
                baseQuery = baseQuery.Where(e => e.Category == catEnum); //
            }

            // normalizacja i ustawienie sortorder dla viewbag
            sortOrder = string.IsNullOrWhiteSpace(sortOrder) ? "date_desc" : sortOrder.ToLower();
            ViewBag.SortOrder = sortOrder; //

            // zmienna pomocnicza do budowania zapytania sortującego
            IQueryable<Expense> queryableItems = baseQuery; //

            // deklaracja zmiennych dla wyników i całkowitej liczby elementów
            List<Expense> items;
            int totalItems;

            // 5) sortowanie - stosowane do queryableitems
            switch (sortOrder)
            {
                case "category_asc":
                    queryableItems = queryableItems.OrderBy(i => i.Category).ThenByDescending(i => i.Date); //
                    break;
                case "category_desc":
                    queryableItems = queryableItems.OrderByDescending(i => i.Category).ThenByDescending(i => i.Date); //
                    break;
                case "date_asc":
                    queryableItems = queryableItems.OrderBy(i => i.Date); //
                    break;
                // przypadki "amount_asc" i "amount_desc" są obsługiwane po switchu
                case "date_desc": // domyślne sortowanie
                default:
                    queryableItems = queryableItems.OrderByDescending(i => i.Date); //
                    // upewniamy się, że viewbag ma poprawną wartość, jeśli sortorder jest nieznany
                    if (sortOrder != "amount_asc" && sortOrder != "amount_desc")
                    {
                        ViewBag.SortOrder = "date_desc"; //
                    }
                    break;
            }

            // oblicz całkowitą liczbę elementów na podstawie queryableitems *przed* pobraniem wszystkiego do pamięci dla sortowania po kwocie
            // lub przed zastosowaniem skip/take dla sortowań bazodanowych
            totalItems = await queryableItems.CountAsync(); //

            // 6) pobieranie danych i paginacja
            if (sortOrder == "amount_asc" || sortOrder == "amount_desc")
            {
                // dla sortowania po kwocie (decimal), które sqlite nie wspiera dobrze w linq-to-entities,
                // pobieramy dane do pamięci i sortujemy po stronie klienta.
                // uwaga: to może być nieefektywne dla bardzo dużych zbiorów danych.
                var allItemsForAmountSort = await queryableItems.ToListAsync(); //

                if (sortOrder == "amount_asc")
                {
                    items = allItemsForAmountSort
                        .OrderBy(i => i.Amount) //
                        .Skip((page - 1) * pageSize) //
                        .Take(pageSize) //
                        .ToList(); //
                }
                else // amount_desc
                {
                    items = allItemsForAmountSort
                        .OrderByDescending(i => i.Amount) //
                        .Skip((page - 1) * pageSize) //
                        .Take(pageSize) //
                        .ToList(); //
                }
            }
            else
            {
                // dla innych sortowań, paginacja jest wykonywana przez bazę danych na już posortowanym queryableitems
                items = await queryableItems
                    .Skip((page - 1) * pageSize) //
                    .Take(pageSize) //
                    .ToListAsync(); //
            }

            // 7) przekazanie danych do widoku za pomocą viewbag
            ViewBag.CurrentPage = page; //
            ViewBag.PageSize = pageSize; //
            ViewBag.TotalItems = totalItems; //
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize); //
            ViewBag.CategoryFilter = categoryFilter; //
            ViewBag.YearFilter = yearFilter; //
            ViewBag.MonthFilter = monthFilter; //

            // lista lat do filtrowania: zawsze zawiera bieżący rok oraz lata z istniejących wydatków użytkownika
            var years = await _context.Expenses
                .Where(e => e.UserId == userId) //
                .Select(e => e.Date.Year) //
                .Distinct() //
                .ToListAsync(); //
            if (!years.Contains(DateTime.Now.Year)) //
            {
                years.Add(DateTime.Now.Year); //
            }
            ViewBag.Years = years.OrderByDescending(y => y).ToList(); //

            // lista miesięcy do filtrowania: zawsze 1-12 z nazwami miesięcy
            ViewBag.Months = Enumerable.Range(1, 12)
                .Select(m => new
                {
                    Number = m,
                    Name = CultureInfo.CurrentCulture?.DateTimeFormat?.GetMonthName(m) ?? m.ToString() //
                })
                .ToList(); //

            return View(items); //
        }

        // ... (pozostałe metody kontrolera: Details, Create GET/POST, Edit GET/POST, Delete GET/POST, ExpenseExists) ...
        // get: wydatki/details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound(); // jeśli id jest null, zwracamy notfound

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //
            if (string.IsNullOrEmpty(userId)) return Challenge(); // sprawdzamy użytkownika

            var expense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId); // szukamy wydatku po id i userid

            if (expense == null) return NotFound(); // jeśli wydatek nie istnieje lub nie należy do użytkownika
            return View(expense); // zwracamy widok ze szczegółami wydatku
        }

        // get: wydatki/create
        public IActionResult Create()
        {
            // inicjalizacja modelu z domyślnymi wartościami dla formularza
            var model = new Expense
            {
                Date = DateTime.Today // ustawiamy dzisiejszą datę jako domyślną
            };
            ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory)); // przekazujemy kategorie do widoku
            return View(model); //
        }

        // post: wydatki/create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Category,Date,Amount,Notes")] Expense expense) // id jest generowane, userid ustawiane poniżej
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //
            if (string.IsNullOrEmpty(userId))
            {
                // ten błąd jest mało prawdopodobny przy [authorize], ale lepiej go obsłużyć
                ModelState.AddModelError(string.Empty, "użytkownik nie jest poprawnie zidentyfikowany. spróbuj zalogować się ponownie."); //
                ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory)); // musimy ponownie załadować kategorie
                return View(expense); // zwróć widok z błędem
            }

            expense.UserId = userId; // przypisz userid do nowego wydatku

            // modelstate.isvalid sprawdzi atrybuty [required] itp. w modelu expense
            if (ModelState.IsValid) //
            {
                _context.Add(expense); //
                await _context.SaveChangesAsync(); //
                return RedirectToAction(nameof(Index)); // przekierowanie do listy po pomyślnym utworzeniu
            }
            // jeśli modelstate nie jest valid, zwróć widok z modelem i błędami walidacji
            ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory)); // musimy ponownie załadować kategorie
            return View(expense); //
        }

        // get: wydatki/edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound(); //

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //
            if (string.IsNullOrEmpty(userId)) return Challenge(); //

            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId); //

            if (expense == null) return NotFound(); // wydatek nie istnieje lub nie należy do użytkownika

            ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory)); // przekazujemy kategorie do widoku
            return View(expense); //
        }

        // post: wydatki/edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Category,Date,Amount,Notes")] Expense expense) //
        {
            if (id != expense.Id) return NotFound(); // sprawdzenie czy id z routingu zgadza się z id w modelu

            string? currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //
            if (string.IsNullOrEmpty(currentUserId))
            {
                ModelState.AddModelError(string.Empty, "nie można zweryfikować użytkownika. spróbuj zalogować się ponownie."); //
                ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory)); // musimy ponownie załadować kategorie
                return View(expense); // zwróć widok z błędem
            }

            var expenseToUpdate = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == currentUserId); //

            if (expenseToUpdate == null)
            {
                return NotFound(); // wydatek nie istnieje lub nie należy do tego użytkownika
            }

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
                return RedirectToAction(nameof(Index)); // przekierowanie do listy po pomyślnej edycji
            }
            ViewBag.Categories = Enum.GetValues(typeof(ExpenseCategory)); // musimy ponownie załadować kategorie
            return View(expenseToUpdate); //
        }

        // get: wydatki/delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound(); //

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //
            if (string.IsNullOrEmpty(userId)) return Challenge(); //

            var expense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId); //

            if (expense == null) return NotFound(); // wydatek nie istnieje lub nie należy do użytkownika
            return View(expense); //
        }

        // post: wydatki/delete/5
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
                TempData["SuccessMessage"] = "wydatek został pomyślnie usunięty."; // komunikat o sukcesie
            }
            else
            {
                TempData["ErrorMessage"] = "nie można usunąć wydatku. nie został znaleziony lub nie masz uprawnień."; // komunikat o błędzie
            }
            return RedirectToAction(nameof(Index)); // przekierowanie do listy
        }

        private bool ExpenseExists(int id, string userId) // dodano parametr userid dla bezpieczeństwa
        {
            return _context.Expenses.Any(e => e.Id == id && e.UserId == userId); //
        }
    }
}