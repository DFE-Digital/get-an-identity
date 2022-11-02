namespace TeacherIdentity.AuthServer;

public static class StringExtensions
{
    public static string? ToNullIfEmpty(this string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
