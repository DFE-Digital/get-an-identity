namespace TeacherIdentity.AuthServer.Journeys;

public abstract class TrnLookupJourneyBase : SignInJourney
{
    protected TrnLookupJourneyBase(HttpContext httpContext, IdentityLinkGenerator linkGenerator)
        : base(httpContext, linkGenerator)
    {
    }
}
