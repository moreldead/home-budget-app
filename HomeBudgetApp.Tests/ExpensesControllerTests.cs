using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using home_budget_app.Controllers;
using home_budget_app.Data;
using home_budget_app.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

public class ExpensesControllerTests
{
    private ExpensesController GetControllerWithContext(ApplicationDbContext context, string userId = "test-user")
    {
        var controller = new ExpensesController(context);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // TempData mock
        controller.TempData = new TempDataDictionary(
            controller.ControllerContext.HttpContext,
            Mock.Of<ITempDataProvider>());

        return controller;
    }

    private async Task<ApplicationDbContext> GetDbContextWithDataAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

        var context = new ApplicationDbContext(options);

        context.Expenses.Add(new Expense { Id = 1, Amount = 100, Category = ExpenseCategory.Food, Date = DateTime.Today, UserId = "test-user" });
        context.Expenses.Add(new Expense { Id = 2, Amount = 50, Category = ExpenseCategory.Other, Date = DateTime.Today.AddDays(-1), UserId = "test-user" });

        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task Index_ReturnsExpenses()
    {
        var context = await GetDbContextWithDataAsync();
        var controller = GetControllerWithContext(context);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<System.Collections.Generic.List<Expense>>(view.Model);
        Assert.Equal(2, model.Count);
    }

    [Fact]
    public async Task Create_Post_ValidExpense_RedirectsToIndex()
    {
        var context = await GetDbContextWithDataAsync();
        var controller = GetControllerWithContext(context);

        var newExpense = new Expense
        {
            Amount = 300,
            Category = ExpenseCategory.Food,
            Date = DateTime.Today,
            Notes = "Test expense"
        };

        var result = await controller.Create(newExpense);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(3, context.Expenses.Count());
    }

    [Fact]
    public async Task Edit_Post_ValidData_SavesChanges()
    {
        var context = await GetDbContextWithDataAsync();
        var controller = GetControllerWithContext(context);

        var update = new Expense
        {
            Id = 1,
            Category = ExpenseCategory.Transport,
            Date = DateTime.Today,
            Amount = 555,
            Notes = "Updated"
        };

        var result = await controller.Edit(1, update);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var updated = await context.Expenses.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Equal(555, updated!.Amount);
        Assert.Equal(ExpenseCategory.Transport, updated.Category);
    }

    [Fact]
    public async Task Details_ExistingId_ReturnsExpense()
    {
        var context = await GetDbContextWithDataAsync();
        var controller = GetControllerWithContext(context);

        var result = await controller.Details(1);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Expense>(view.Model);
        Assert.Equal(1, model.Id);
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesExpense()
    {
        var context = await GetDbContextWithDataAsync();
        var controller = GetControllerWithContext(context);

        var result = await controller.DeleteConfirmed(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Single(context.Expenses); // zostało tylko 1
    }
}
