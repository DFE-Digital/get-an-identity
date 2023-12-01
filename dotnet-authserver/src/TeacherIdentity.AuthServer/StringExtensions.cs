namespace TeacherIdentity.AuthServer;

public static class StringExtensions
{
    public static string? ToNullIfEmpty(this string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    public static string Truncate(this string value, int maxLength) => value.Length > maxLength ? value[..maxLength] : value;
}
