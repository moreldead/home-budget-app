using Microsoft.AspNetCore.Mvc;
using home_budget_app.Data;
using home_budget_app.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace home_budget_app.Controllers
{
    public class TestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Test
        public async Task<IActionResult> Index()
        {
            // Retrieve all categories
            var categories = await _context.Categories.ToListAsync();

            return View(categories);
        }

        // GET: /Test/CreateSampleData
        public IActionResult CreateSampleData()
        {
            // Add a test category if none exist
            if (!_context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Housing", Description = "Rent, mortgage, repairs, etc." },
                    new Category { Name = "Groceries", Description = "Food and household items" },
                    new Category { Name = "Transportation", Description = "Car payments, gas, public transit" },
                    new Category { Name = "Entertainment", Description = "Movies, dining out, hobbies" }
                };

                _context.Categories.AddRange(categories);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Test/AddNewCategory
        public IActionResult AddNewCategory()
        {
            // Get all existing category names
            var existingNames = _context.Categories.Select(c => c.Name).ToList();

            // Generate a unique name for the new category
            string baseName = "Test Category";
            string newName = baseName;
            int counter = 1;

            while (existingNames.Contains(newName))
            {
                newName = $"{baseName} {counter}";
                counter++;
            }

            // Create and add the new category
            var newCategory = new Category
            {
                Name = newName,
                Description = $"This is a test category added on {DateTime.Now.ToShortDateString()}"
            };

            _context.Categories.Add(newCategory);
            _context.SaveChanges();

            TempData["Message"] = $"Added new category: {newName}";

            return RedirectToAction(nameof(Index));
        }
    }
}