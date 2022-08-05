using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TeacherIdentity.AuthServer;

public static class EnumExtensions
{
    public static string? GetDisplayName<T>(this T value)
        where T : Enum
    {
        // TODO Cache the results of this

        var enumType = typeof(T);
        var enumValue = Enum.GetName(enumType, value)!;
        var member = enumType.GetMember(enumValue)[0];

        var displayAttr = member.GetCustomAttribute<DisplayAttribute>();
        return displayAttr?.Name;
    }
}
