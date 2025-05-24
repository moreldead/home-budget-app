using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection; // potrzebne dla getfield

namespace home_budget_app.Extensions
{
    public static class EnumExtensions
    {
        // rozszerzenie dla wszystkich enumów, które obsługują atrybut display
        public static string GetDisplayName(this Enum enumValue)
        {
            if (enumValue == null) // dodano sprawdzenie null dla samego enumvalue
            {
                return string.Empty;
            }

            FieldInfo? field = enumValue.GetType().GetField(enumValue.ToString()); // pole może być null

            if (field == null) // poprawka - sprawdź czy pole istnieje
            {
                return enumValue.ToString(); // zwróć domyślną nazwę, jeśli pole nie zostało znalezione
            }

            DisplayAttribute? attribute = (DisplayAttribute?)Attribute.GetCustomAttribute(field, typeof(DisplayAttribute)); // atrybut może być null

            return attribute?.Name ?? enumValue.ToString(); // zwraca nazwę przypisaną w atrybucie display lub nazwę enumu
        }
    }
}