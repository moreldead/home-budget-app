using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using home_budget_app.Data;
using home_budget_app.Models; // <<< --- ADD THIS USING DIRECTIVE

namespace home_budget_app;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // Use ApplicationUser here
        builder.Services.AddDefaultIdentity<ApplicationUser>(options => // <--- CHANGE IdentityUser to ApplicationUser
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

        // Configure the HTTP request pipeline.
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

        app.UseAuthorization(); // Make sure this comes after UseRouting

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.MapRazorPages(); // If you have scaffolded Identity UI, it uses Razor Pages

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ApplicationDbContext>();
            // Use ApplicationUser here as well
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>(); // <--- CHANGE IdentityUser to ApplicationUser

            // Ensure your DbSeeder.Seed method's signature matches this UserManager type
            DbSeeder.Seed(context, userManager);
        }

        app.Run();
    }
}