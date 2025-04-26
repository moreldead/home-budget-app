using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace home_budget_app.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<Expense>? Expenses { get; set; }
        public virtual ICollection<Budget>? Budgets { get; set; }
    }
}