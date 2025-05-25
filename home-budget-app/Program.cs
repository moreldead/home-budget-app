using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using home_budget_app.Data;
using home_budget_app.Models;

namespace home_budget_app;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // uzywaj ApplicationUser
        builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false; // Set to true in production if needed
            options.SignIn.RequireConfirmedEmail = false;  // Set to true in production if needed
        })
            .AddEntityFrameworkStores<ApplicationDbContext>();

        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("Content-Type", "text/html; charset=utf-8");
            await next();
        });

        // konfiguracja pipelinu http request
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.MapRazorPages();

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ApplicationDbContext>();
            
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            
            DbSeeder.Seed(context, userManager);
        }

        app.Run();
    }
}