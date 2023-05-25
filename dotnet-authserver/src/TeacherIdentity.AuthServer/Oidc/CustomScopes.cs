namespace TeacherIdentity.AuthServer.Oidc;

public static class CustomScopes
{
    public const string GetAnIdentitySupport = "get-an-identity:support";
    public const string UserRead = "user:read";
    public const string UserWrite = "user:write";
    public const string DqtRead = "dqt:read";
    public const string Trn = "trn";
    public const string TrnTokenWrite = "trn_token:write";

    public static string[] All => StaffUserTypeScopes.Concat(DefaultUserTypesScopes).Concat(NonUserLevelScopes).ToArray();

    public static string[] NonUserLevelScopes { get; } = new[]
    {
        TrnTokenWrite
    };

    public static string[] StaffUserTypeScopes { get; } = new[]
    {
        GetAnIdentitySupport,
        UserRead,
        UserWrite
    };

    public static string[] DefaultUserTypesScopes { get; } = new[]
    {
        DqtRead,
        Trn
    };
}
