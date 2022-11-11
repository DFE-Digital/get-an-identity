namespace TeacherIdentity.AuthServer.Oidc;

public static class CustomScopes
{
    public const string GetAnIdentitySupport = "get-an-identity:support";
    public const string UserRead = "user:read";
    public const string UserWrite = "user:write";
    public const string Trn = "trn";

    public static string[] All => StaffUserTypeScopes.Concat(DefaultUserTypesScopes).ToArray();

    public static string[] StaffUserTypeScopes { get; } = new[]
    {
        GetAnIdentitySupport,
        UserRead,
        UserWrite
    };

    public static string[] DefaultUserTypesScopes { get; } = new[]
    {
        Trn
    };
}
