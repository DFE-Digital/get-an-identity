namespace TeacherIdentity.AuthServer.Oidc;

public static class CustomClaims
{
    public const string DateFormat = "yyyy-MM-dd";

    public const string Trn = "trn";
    public const string TrnLookupStatus = "trn_lookup_status";
    public const string UserType = "user_type";
    public const string PreviousUserId = "previous_user_id";
    public const string PreferredName = "preferred_name";
    public const string NiNumber = "ni_number";
    public const string TrnMatchNiNumber = "trn_match_ni_number";

    public static class Private
    {
        public const string TrnMatchPolicy = "gai:trn_match_policy";
    }
}
