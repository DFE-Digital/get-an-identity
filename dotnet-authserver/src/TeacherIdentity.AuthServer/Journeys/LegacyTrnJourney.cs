using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Journeys;

public class LegacyTrnJourney : TrnLookupJourneyBase
{
    public LegacyTrnJourney(
        AuthenticationState authenticationState,
        HttpContext httpContext,
        IdentityLinkGenerator linkGenerator)
        : base(authenticationState, httpContext, linkGenerator)
    {
    }

    public override bool IsFinished()
    {
        throw new NotImplementedException();
    }

    protected override Task<IActionResult> OnEmailVerifiedCore(User? user)
    {
        throw new NotImplementedException();
    }
}
