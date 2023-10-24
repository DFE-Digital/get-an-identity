namespace TeacherIdentity.AuthServer;

public static class AuthorizationPolicies
{
    public const string ApiTrnTokenWrite = "API:TrnTokenWrite";
    public const string ApiUserRead = "API:UserRead";
    public const string ApiUserWrite = "API:UserWrite";
    public const string Authenticated = "Authenticated";
    public const string GetAnIdentityAdmin = "GetAnIdentityAdmin";
    public const string GetAnIdentitySupport = "GetAnIdentitySupport";
    public const string Staff = "Staff";
    public const string TrnLookupApi = "API:TrnLookup";
}
