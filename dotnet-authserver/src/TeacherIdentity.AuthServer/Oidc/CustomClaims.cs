namespace TeacherIdentity.AuthServer.Oidc;

public static class CustomClaims
{
    public const string DateFormat = "yyyy-MM-dd";

    public const string Trn = "trn";
    public const string TrnLookupStatus = "trn-lookup-status";
    public const string HaveCompletedTrnLookup = "completed-trn-lookup";
    public const string UserType = "user-type";
}
