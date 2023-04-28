namespace TeacherIdentity.AuthServer.Journeys;

public class StaffSignInJourney : SignInJourney
{
    public StaffSignInJourney(HttpContext httpContext, IdentityLinkGenerator linkGenerator, CreateUserHelper createUserHelper)
        : base(httpContext, linkGenerator, createUserHelper)
    {
    }

    public override bool CanAccessStep(string step)
    {
        throw new NotImplementedException();
    }

    protected override string? GetNextStep(string currentStep)
    {
        throw new NotImplementedException();
    }

    protected override string? GetPreviousStep(string currentStep)
    {
        throw new NotImplementedException();
    }

    protected override string GetStartStep()
    {
        throw new NotImplementedException();
    }

    protected override string GetStepUrl(string step)
    {
        throw new NotImplementedException();
    }

    protected override bool IsFinished()
    {
        throw new NotImplementedException();
    }
}
