using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using home_budget_app.Data;
using home_budget_app.Models;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace home_budget_app;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>();

        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();

        // Konfiguracja obsługi kultury (pl-PL)
        var supportedCultures = new[] { new CultureInfo("pl-PL") };
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            var culture = new CultureInfo("pl-PL");
            options.DefaultRequestCulture = new RequestCulture(culture);
            options.SupportedCultures = new[] { culture };
            options.SupportedUICultures = new[] { culture };
            options.RequestCultureProviders.Clear(); // <-- usuwa wykrywanie kultury z nagłówków, cookies itd.
        });


        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pl-PL");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("pl-PL");

        var app = builder.Build();

        // Middleware lokalizacji (MUSI być przed UseRouting i innymi)
        var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        app.UseRequestLocalization(locOptions);

        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("Content-Type", "text/html; charset=utf-8");
            await next();
        });

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

        app.UseAuthentication(); // brakowało tego w Twoim kodzie, jeśli używasz Identity
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.MapRazorPages();

        // Seeder danych
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
