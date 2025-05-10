using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace home_budget_app.Extensions
{
    public static class EnumExtensions
    {
        // Rozszerzenie dla wszystkich enumów, które obsługują atrybut Display
        public static string GetDisplayName(this Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            var attribute = (DisplayAttribute)Attribute.GetCustomAttribute(field, typeof(DisplayAttribute));

            return attribute?.Name ?? enumValue.ToString(); // Zwraca nazwę przypisaną w atrybucie Display lub nazwę enumu
        }
    }
}
