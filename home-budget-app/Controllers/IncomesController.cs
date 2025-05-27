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
    public class IncomesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IncomesController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // get: przychody - lista z paginacją, sortowaniem i filtrami
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            string sortOrder = "date_desc",
            string categoryFilter = "",
            int? yearFilter = null,
            int? monthFilter = null)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            IQueryable<Income> baseQuery = _context.Incomes.Where(i => i.UserId == userId); 

            if (yearFilter.HasValue)
            {
                baseQuery = baseQuery.Where(i => i.Date.Year == yearFilter.Value); 
            }
            if (monthFilter.HasValue)
            {
                baseQuery = baseQuery.Where(i => i.Date.Month == monthFilter.Value); 
            }
            if (!string.IsNullOrEmpty(categoryFilter) && Enum.TryParse<IncomeCategory>(categoryFilter, out var catEnum))
            {
                baseQuery = baseQuery.Where(i => i.Category == catEnum); 
            }

            sortOrder = string.IsNullOrWhiteSpace(sortOrder) ? "date_desc" : sortOrder.ToLower();
            ViewBag.SortOrder = sortOrder; 

            IQueryable<Income> queryableItems = baseQuery;
            List<Income> items;
            int totalItems;

            switch (sortOrder)
            {
                case "id_asc":
                    queryableItems = queryableItems.OrderBy(i => i.Id);
                    break;
                case "id_desc":
                    queryableItems = queryableItems.OrderByDescending(i => i.Id);
                    break;
                case "category_asc":
                    queryableItems = queryableItems.OrderBy(i => i.Category).ThenByDescending(i => i.Date); 
                    break;
                case "category_desc":
                    queryableItems = queryableItems.OrderByDescending(i => i.Category).ThenByDescending(i => i.Date); 
                    break;
                case "date_asc":
                    queryableItems = queryableItems.OrderBy(i => i.Date); 
                    break;
                case "date_desc":
                default:
                    queryableItems = queryableItems.OrderByDescending(i => i.Date); 
                    if (sortOrder != "amount_asc" && sortOrder != "amount_desc" && sortOrder != "id_asc" && sortOrder != "id_desc") 
                    {
                        ViewBag.SortOrder = "date_desc";
                    }
                    break;
            }

            totalItems = await queryableItems.CountAsync();

            if (sortOrder == "amount_asc" || sortOrder == "amount_desc")
            {
                var allItemsForAmountSort = await queryableItems.ToListAsync();
                if (sortOrder == "amount_asc")
                {
                    items = allItemsForAmountSort.OrderBy(i => i.Amount).Skip((page - 1) * pageSize).Take(pageSize).ToList(); 
                }
                else
                {
                    items = allItemsForAmountSort.OrderByDescending(i => i.Amount).Skip((page - 1) * pageSize).Take(pageSize).ToList(); 
                }
            }
            else
            {
                items = await queryableItems.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(); 
            }

            ViewBag.CurrentPage = page; 
            ViewBag.PageSize = pageSize; 
            ViewBag.TotalItems = totalItems; 
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize); 
            ViewBag.CategoryFilter = categoryFilter; 
            ViewBag.YearFilter = yearFilter; 
            ViewBag.MonthFilter = monthFilter; 

            var years = await _context.Incomes.Where(i => i.UserId == userId).Select(i => i.Date.Year).Distinct().ToListAsync(); 
            if (!years.Contains(DateTime.Now.Year)) 
            {
                years.Add(DateTime.Now.Year); 
            }
            ViewBag.Years = years.OrderByDescending(y => y).ToList(); 

            ViewBag.Months = Enumerable.Range(1, 12).Select(m => new { Number = m, Name = CultureInfo.CurrentCulture?.DateTimeFormat?.GetMonthName(m) ?? m.ToString() }).ToList(); 

            return View(items); 
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var income = await _context.Incomes
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (income == null) return NotFound();
            return View(income);
        }
        

        // get: przychody/create
        public IActionResult Create() 
        {
            var model = new Income
            {
                Date = DateTime.Today 
            };
            ViewBag.Categories = Enum.GetValues(typeof(IncomeCategory)); 
            return View(model); 
        }

        // post: przychody/create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Category,Date,Amount,Notes")] Income income) 
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; 
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError(string.Empty, "użytkownik nie jest poprawnie zidentyfikowany. spróbuj zalogować się ponownie."); 
                ViewBag.Categories = Enum.GetValues(typeof(IncomeCategory));
                return View(income); 
            }

            income.UserId = userId; 

            if (ModelState.IsValid) 
            {
                _context.Add(income); 
                await _context.SaveChangesAsync(); 
                return RedirectToAction(nameof(Index)); 
            }
            ViewBag.Categories = Enum.GetValues(typeof(IncomeCategory));
            return View(income); 
        }

        // get: przychody/edit/5
        public async Task<IActionResult> Edit(int? id) 
        {
            if (id == null) return NotFound(); 

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; 
            if (string.IsNullOrEmpty(userId)) return Challenge(); 

            var income = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId); 

            if (income == null) return NotFound(); 
            ViewBag.Categories = Enum.GetValues(typeof(IncomeCategory)); 
            return View(income); 
        }

        // post: przychody/edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Category,Date,Amount,Notes")] Income income) 
        {
            if (id != income.Id) return NotFound(); 

            string? currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; 
            if (string.IsNullOrEmpty(currentUserId))
            {
                ModelState.AddModelError(string.Empty, "nie można zweryfikować użytkownika. spróbuj zalogować się ponownie."); 
                ViewBag.Categories = Enum.GetValues(typeof(IncomeCategory));
                return View(income); 
            }

            var incomeToUpdate = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == currentUserId); 

            if (incomeToUpdate == null)
            {
                return NotFound(); 
            }

            incomeToUpdate.Category = income.Category; 
            incomeToUpdate.Date = income.Date; 
            incomeToUpdate.Amount = income.Amount; 
            incomeToUpdate.Notes = income.Notes; 

            if (ModelState.IsValid) 
            {
                try
                {
                    await _context.SaveChangesAsync(); 
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IncomeExists(incomeToUpdate.Id, currentUserId)) return NotFound(); 
                    else throw; 
                }
                return RedirectToAction(nameof(Index)); 
            }
            ViewBag.Categories = Enum.GetValues(typeof(IncomeCategory));
            return View(incomeToUpdate); 
        }

        // get: przychody/delete/5
        public async Task<IActionResult> Delete(int? id) 
        {
            if (id == null) return NotFound(); 

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; 
            if (string.IsNullOrEmpty(userId)) return Challenge(); 

            var income = await _context.Incomes
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId); 

            if (income == null) return NotFound(); 
            return View(income); //
        }

        // post: przychody/delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id) 
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; 
            if (string.IsNullOrEmpty(userId)) return Challenge(); 

            var income = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId); 

            if (income != null) 
            {
                _context.Incomes.Remove(income); 
                await _context.SaveChangesAsync(); 
                TempData["SuccessMessage"] = "przychód został pomyślnie usunięty."; 
            }
            else
            {
                TempData["ErrorMessage"] = "nie można usunąć przychodu. nie został znaleziony lub nie masz uprawnień."; 
            }
            return RedirectToAction(nameof(Index)); 
        }

        private bool IncomeExists(int id, string userId) 
        {
            return _context.Incomes.Any(i => i.Id == id && i.UserId == userId); 
        }
    }
}