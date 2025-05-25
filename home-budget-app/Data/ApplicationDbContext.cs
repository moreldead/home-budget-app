using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using home_budget_app.Models;

namespace home_budget_app.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Budget management entities
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Income> Incomes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Keep this line to maintain Identity tables configuration

            // Configure relationships and constraints

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

            // konfiguracja relacji dla expense
            // In ApplicationDbContext.OnModelCreating method:

            // Relationship for Expense
            builder.Entity<Expense>()
                .HasOne(e => e.User)               // Expense has one User
                .WithMany(u => u.Expenses)       // User has many Expenses (using the Expenses collection)
                .HasForeignKey(e => e.UserId)
                .IsRequired(false); // UserId is nullable, so this is fine

            // Relationship for Income
            builder.Entity<Income>()
                .HasOne(i => i.User)               // Income has one User
                .WithMany(u => u.Incomes)        // User has many Incomes (using the Incomes collection)
                .HasForeignKey(i => i.UserId)
                .IsRequired(false); // UserId is nullable, so this is fine
        }
    }
}