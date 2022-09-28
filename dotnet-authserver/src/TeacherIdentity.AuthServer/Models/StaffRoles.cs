namespace TeacherIdentity.AuthServer.Models;

public static class StaffRoles
{
    public const string GetAnIdentityAdmin = "GetAnIdentityAdmin";
    public const string GetAnIdentitySupport = "GetAnIdentitySupport";

    public static string[] All { get; } = new[]
    {
        GetAnIdentityAdmin,
        GetAnIdentitySupport
    };

    public static string[] None { get; } = Array.Empty<string>();

    public static string GetRoleLabel(string roleName) => roleName switch
    {
        GetAnIdentityAdmin => "Get an identity admin",
        GetAnIdentitySupport => "Get an identity support",
        _ => throw new ArgumentException("Unknown role.", nameof(roleName))
    };
}
