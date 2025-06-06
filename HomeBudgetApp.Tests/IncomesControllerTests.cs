using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using home_budget_app.Controllers;
using home_budget_app.Data;
using home_budget_app.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

public class IncomesControllerTests
{
    private IncomesController GetControllerWithContext(ApplicationDbContext context, string userId = "test-user")
    {
        var controller = new IncomesController(context);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
        new Claim(ClaimTypes.NameIdentifier, userId)
    }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Dodaj mock TempData
        var tempData = new TempDataDictionary(
            controller.ControllerContext.HttpContext,
            Mock.Of<ITempDataProvider>());
        controller.TempData = tempData;

        return controller;
    }


    private async Task<ApplicationDbContext> GetDbContextWithDataAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

        var context = new ApplicationDbContext(options);

        context.Incomes.Add(new Income { Id = 1, Amount = 100, Category = IncomeCategory.Salary, Date = DateTime.Today, UserId = "test-user" });
        context.Incomes.Add(new Income { Id = 2, Amount = 50, Category = IncomeCategory.Other, Date = DateTime.Today.AddDays(-1), UserId = "test-user" });

        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task Index_ReturnsViewWithModel()
    {
        var context = await GetDbContextWithDataAsync();
        var controller = GetControllerWithContext(context);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<System.Collections.Generic.List<Income>>(viewResult.Model);
        Assert.Equal(2, model.Count);
    }

    [Fact]
    public async Task Create_Post_ValidModel_RedirectsToIndex()
    {
        var context = await GetDbContextWithDataAsync();
        var controller = GetControllerWithContext(context);

        var newIncome = new Income
        {
            Amount = 200,
            Date = DateTime.Today,
            Category = IncomeCategory.Other,
            Notes = "Test"
        };

        var result = await controller.Create(newIncome);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        Assert.Equal(3, context.Incomes.Count());
    }

    [Fact]
    public async Task Edit_Get_ReturnsViewWithModel()
    {
        var context = await GetDbContextWithDataAsync();
        var controller = GetControllerWithContext(context);

        var result = await controller.Edit(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Income>(viewResult.Model);
        Assert.Equal(1, model.Id);
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesIncomeAndRedirects()
    {
        var context = await GetDbContextWithDataAsync();
        var controller = GetControllerWithContext(context);

        var result = await controller.DeleteConfirmed(1);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal(1, context.Incomes.Count()); // było 2, usunięto 1
    }

    [Fact]
    public async Task Details_ValidId_ReturnsCorrectIncome()
    {
        var context = await GetDbContextWithDataAsync();
        var controller = GetControllerWithContext(context);

        var result = await controller.Details(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Income>(viewResult.Model);
        Assert.Equal(1, model.Id);
    }

    [Fact]
    public async Task Edit_Post_ValidChanges_SavesAndRedirects()
    {
        var context = await GetDbContextWithDataAsync();
        var controller = GetControllerWithContext(context);

        var income = new Income
        {
            Id = 1,
            Category = IncomeCategory.Other,
            Date = DateTime.Today,
            Amount = 999,
            Notes = "Updated"
        };

        var result = await controller.Edit(1, income);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var updated = await context.Incomes.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Equal(999, updated!.Amount);

        Assert.Equal(IncomeCategory.Other, updated.Category);
    }
}
