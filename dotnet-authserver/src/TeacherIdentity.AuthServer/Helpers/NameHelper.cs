namespace TeacherIdentity.AuthServer.Helpers;

public static class NameHelper
{
    public static string? GetFullName(string? firstName, string? middleName, string? lastName)
    {
        if (firstName is null || lastName is null)
        {
            return null;
        }

        return middleName is null ? $"{firstName} {lastName}" : $"{firstName} {middleName} {lastName}";
    }
}
