namespace TeacherIdentity.AuthServer.Infrastructure.Security;

public static class AuthorizationPolicies
{
    public const string GetAnIdentityAdmin = "GetAnIdentityAdmin";
    public const string GetAnIdentitySupportApi = "API:GetAnIdentitySupport";
    public const string TrnLookupApi = "API:TrnLookup";
}
