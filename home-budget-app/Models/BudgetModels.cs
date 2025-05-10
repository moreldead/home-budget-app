using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace home_budget_app.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Kategoria")]
        public ExpenseCategory Category { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Data")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [Range(0.01, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Kwota")]
        public decimal Amount { get; set; }

        [StringLength(500)]
        [Display(Name = "Komentarz")]
        public string? Notes { get; set; }

        public string? UserId { get; set; }
    }

    public class Income
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Kategoria")]
        public IncomeCategory Category { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Data")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [Range(0.01, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Kwota")]
        public decimal Amount { get; set; }

        [StringLength(500)]
        [Display(Name = "Komentarz")]
        public string? Notes { get; set; }

        public string? UserId { get; set; }
    }

    public enum ExpenseCategory
    {
        [Display(Name = "Zakupy codzienne")]
        Food,

        [Display(Name = "Opłaty i rachunki")]
        Bills,

        [Display(Name = "Czynsz")]
        Rent,

        [Display(Name = "Transport")]
        Transport,

        [Display(Name = "Rozrywka")]
        Entertainment,

        [Display(Name = "Zdrowie")]
        Health,

        [Display(Name = "Długi")]
        Debt,

        [Display(Name = "Ubrania")]
        Clothes,

        [Display(Name = "Wakacje")]
        Holidays,

        [Display(Name = "Subskrypcje")]
        Subscriptions,

        [Display(Name = "Samochód")]
        Car,

        [Display(Name = "Inne")]
        Other
    }

    public enum IncomeCategory
    {
        [Display(Name = "Wynagrodzenie")]
        Salary,

        [Display(Name = "Premia")]
        Bonus,

        [Display(Name = "Dywidendy")]
        Dividends,

        [Display(Name = "Inne")]
        Other
    }

    public class ExpenseSummary
    {
        public string Category { get; set; }
        public double Sum { get; set; }
    }

    public class IncomeSummary
    {
        public string Category { get; set; }
        public double Sum { get; set; }
    }

}
