using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using home_budget_app.Models;

namespace home_budget_app.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Budget management entities
        public DbSet<Category> Categories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Income> Incomes { get; set; }
        public DbSet<Budget> Budgets { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Keep this line to maintain Identity tables configuration

            // Configure relationships and constraints

            // Category configuration
            builder.Entity<Category>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();

                // Each category can have many expenses
                entity.HasMany(c => c.Expenses)
                      .WithOne(e => e.Category)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Each category can have one budget
                entity.HasMany(c => c.Budgets)
                      .WithOne(b => b.Category)
                      .HasForeignKey(b => b.CategoryId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Expense configuration
            builder.Entity<Expense>(entity =>
            {
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Date).IsRequired();
            });

            // Income configuration
            builder.Entity<Income>(entity =>
            {
                entity.Property(i => i.Amount).HasColumnType("decimal(18,2)");
                entity.Property(i => i.Date).IsRequired();
            });

            // Budget configuration
            builder.Entity<Budget>(entity =>
            {
                entity.Property(b => b.Amount).HasColumnType("decimal(18,2)");
            });

            // Add some default categories with proper initialization
            builder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Housing", Description = "Rent, mortgage, repairs, etc." },
                new Category { Id = 2, Name = "Utilities", Description = "Electricity, water, internet, etc." },
                new Category { Id = 3, Name = "Groceries", Description = "Food and household items" },
                new Category { Id = 4, Name = "Transportation", Description = "Car payments, gas, public transit" },
                new Category { Id = 5, Name = "Entertainment", Description = "Movies, dining out, hobbies" },
                new Category { Id = 6, Name = "Health", Description = "Medical expenses, insurance, etc." },
                new Category { Id = 7, Name = "Debt", Description = "Credit card payments, loans, etc." },
                new Category { Id = 8, Name = "Savings", Description = "Emergency fund, investments, etc." }
            );
        }
    }
}