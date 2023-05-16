namespace TeacherIdentity.AuthServer.Journeys;

public class TrnTokenSignInJourney : SignInJourney
{
    public TrnTokenSignInJourney(
        HttpContext httpContext,
        IdentityLinkGenerator linkGenerator,
        CreateUserHelper createUserHelper)
        : base(httpContext, linkGenerator, createUserHelper)
    {
    }

    public override bool CanAccessStep(string step)
    {
        return step switch
        {
            Steps.Landing => true,
            _ => false
        };
    }

    protected override string? GetNextStep(string currentStep)
    {
        return (currentStep, AuthenticationState) switch
        {
            _ => null
        };
    }

    protected override string? GetPreviousStep(string currentStep) => (currentStep, AuthenticationState) switch
    {
        _ => null
    };

    protected override string GetStartStep() => Steps.Landing;

    protected override bool IsFinished() => AuthenticationState.IsComplete;

    protected override string GetStepUrl(string step) => step switch
    {
        Steps.Landing => LinkGenerator.TrnTokenLanding(),
        _ => throw new ArgumentException($"Unknown step: '{step}'.")
    };

    public new static class Steps
    {
        public const string Landing = $"{nameof(TrnTokenSignInJourney)}.{nameof(Landing)}";
    }
}
