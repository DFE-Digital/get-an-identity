namespace TeacherIdentity.AuthServer.Journeys;

public abstract class TrnLookupJourneyBase : SignInJourney
{
    protected TrnLookupJourneyBase(
        AuthenticationState authenticationState,
        HttpContext httpContext,
        IdentityLinkGenerator linkGenerator)
        : base(authenticationState, httpContext, linkGenerator)
    {
    }
}
