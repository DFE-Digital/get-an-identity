namespace TeacherIdentity.AuthServer.Infrastructure.Security;

public static class AuthorizationPolicies
{
    public const string ApiUserRead = "API:UserRead";
    public const string ApiUserWrite = "API:UserWrite";
    public const string Authenticated = "Authenticated";
    public const string GetAnIdentityAdmin = "GetAnIdentityAdmin";
    public const string GetAnIdentitySupport = "GetAnIdentitySupport";
    public const string Staff = "Staff";
    public const string TrnLookupApi = "API:TrnLookup";
    public const string TrnTokenApi = "API:TrnToken";
}
