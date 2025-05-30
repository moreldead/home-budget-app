using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace home_budget_app.Models
{
    public class ApplicationUser : IdentityUser
    {
        public virtual ICollection<Expense> Expenses { get; set; } = new HashSet<Expense>();
        public virtual ICollection<Income> Incomes { get; set; } = new HashSet<Income>();
    }

    public class Expense
    {
        public int Id { get; set; }

        [Required, Display(Name = "Kategoria")]
        public ExpenseCategory Category { get; set; }

        [Required, DataType(DataType.Date), Display(Name = "Data")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [Column(TypeName = "decimal(18,2)"), Display(Name = "Kwota")]
        [Range(typeof(decimal), "0,01", "10000", ErrorMessage = "Kwota musi być z zakresu od 0,01 do 10000")]
        public decimal Amount { get; set; }


        [StringLength(500), Display(Name = "Komentarz")]
        public string? Notes { get; set; }

        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }
    }

    public class Income
    {
        public int Id { get; set; }

        [Required, Display(Name = "Kategoria")]
        public IncomeCategory Category { get; set; }

        [Required, DataType(DataType.Date), Display(Name = "Data")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [Column(TypeName = "decimal(18,2)"), Display(Name = "Kwota")]
        [Range(typeof(decimal), "0,01", "10000", ErrorMessage = "Kwota musi być z zakresu od 0,01 do 10000")]
        public decimal Amount { get; set; }

        [StringLength(500), Display(Name = "Komentarz")]
        public string? Notes { get; set; }

        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }
    }

    public enum ExpenseCategory
    {
        [Display(Name = "Zakupy codzienne")] Food,
        [Display(Name = "Opłaty i rachunki")] Bills,
        [Display(Name = "Czynsz")] Rent,
        [Display(Name = "Transport")] Transport,
        [Display(Name = "Rozrywka")] Entertainment,
        [Display(Name = "Zdrowie")] Health,
        [Display(Name = "Długi")] Debt,
        [Display(Name = "Ubrania")] Clothes,
        [Display(Name = "Wakacje")] Holidays,
        [Display(Name = "Subskrypcje")] Subscriptions,
        [Display(Name = "Samochód")] Car,
        [Display(Name = "Inne")] Other
    }

    public enum IncomeCategory
    {
        [Display(Name = "Wynagrodzenie")] Salary,
        [Display(Name = "Premia")] Bonus,
        [Display(Name = "Dywidendy")] Dividends,
        [Display(Name = "Inne")] Other
    }

    public class ExpenseSummary
    {
        public string? Category { get; set; }
        public decimal Sum { get; set; }
    }

    public class IncomeSummary
    {
        public string? Category { get; set; }
        public decimal Sum { get; set; }
    }

    public class MonthlySummaryViewModel
    {
        public int MonthNumber { get; set; }
        public string MonthName { get; set; }
        public double TotalExpenses { get; set; } // Matches the 'double' type used in your controller sums
        public double TotalIncomes { get; set; }  // Matches the 'double' type used in your controller sums
    }

}
