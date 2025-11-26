namespace TeacherIdentity.AuthServer.Models;

public static class StaffRoles
{
    public const string GetAnIdentityAdmin = "GetAnIdentityAdmin";
    public const string GetAnIdentitySupport = "GetAnIdentitySupport";
    public const string GetAnIdentitySupportMergeUser = "GetAnIdentitySupportMergeUser";

    public static string[] All { get; } = new[]
    {
        GetAnIdentityAdmin,
        GetAnIdentitySupport,
        GetAnIdentitySupportMergeUser
    };

    public static string[] None { get; } = Array.Empty<string>();

    public static string GetRoleLabel(string roleName) => roleName switch
    {
        GetAnIdentityAdmin => "Get an identity admin",
        GetAnIdentitySupport => "Get an identity support",
        GetAnIdentitySupportMergeUser => "Get an identity support merge user",
        _ => throw new ArgumentException("Unknown role.", nameof(roleName))
    };
}
