using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Journeys;

public class CoreSignInJourney : SignInJourney
{
    public CoreSignInJourney(HttpContext httpContext, IdentityLinkGenerator linkGenerator)
        : base(httpContext, linkGenerator)
    {
    }

    public override bool CanAccessStep(string step)
    {
        throw new NotImplementedException();
    }

    public override string? GetNextStep(string currentStep)
    {
        throw new NotImplementedException();
    }

    public override string? GetPreviousStep(string currentStep)
    {
        throw new NotImplementedException();
    }

    public override string GetStartStep()
    {
        throw new NotImplementedException();
    }

    public override string GetStepUrl(string step)
    {
        throw new NotImplementedException();
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
