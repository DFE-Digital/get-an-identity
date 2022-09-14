namespace TeacherIdentity.AuthServer.Oidc;

public static class CustomScopes
{
    public const string GetAnIdentityAdmin = "get-an-identity:admin";
    public const string GetAnIdentitySupport = "get-an-identity:support";
    public const string Trn = "trn";

    public static string[] All => AdminScopes.Concat(TeacherScopes).ToArray();

    public static string[] AdminScopes { get; } = new[]
    {
        GetAnIdentityAdmin,
        GetAnIdentitySupport
    };

    public static string[] TeacherScopes { get; } = new[]
    {
        Trn
    };
}
