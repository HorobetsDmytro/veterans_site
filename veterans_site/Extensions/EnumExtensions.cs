using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace veterans_site.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            var enumMember = enumValue.GetType().GetMember(enumValue.ToString());
            var displayAttribute = enumMember.FirstOrDefault()?.GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Name ?? enumValue.ToString();
        }
    }
}
