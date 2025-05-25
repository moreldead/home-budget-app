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

        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Income> Incomes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); 


            builder.Entity<Expense>(entity =>
            {
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Date).IsRequired();
            });

            builder.Entity<Income>(entity =>
            {
                entity.Property(i => i.Amount).HasColumnType("decimal(18,2)");
                entity.Property(i => i.Date).IsRequired();
            });

            // konfiguracja relacji dla expense

            builder.Entity<Expense>()
                .HasOne(e => e.User)               
                .WithMany(u => u.Expenses)       
                .HasForeignKey(e => e.UserId)
                .IsRequired(false); 

    
            builder.Entity<Income>()
                .HasOne(i => i.User)               
                .WithMany(u => u.Incomes)        
                .HasForeignKey(i => i.UserId)
                .IsRequired(false); 
        }
    }
}