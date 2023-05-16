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
            CoreSignInJourney.Steps.Phone => true,
            CoreSignInJourney.Steps.PhoneConfirmation => AuthenticationState is { MobileNumberSet: true, MobileNumberVerified: false },
            _ => false
        };
    }

    protected override string? GetNextStep(string currentStep)
    {
        return (currentStep, AuthenticationState) switch
        {
            (CoreSignInJourney.Steps.Phone, _) => CoreSignInJourney.Steps.PhoneConfirmation,
            _ => null
        };
    }

    protected override string? GetPreviousStep(string currentStep) => (currentStep, AuthenticationState) switch
    {
        (CoreSignInJourney.Steps.Phone, _) => Steps.Landing,
        (CoreSignInJourney.Steps.PhoneConfirmation, _) => CoreSignInJourney.Steps.Phone,
        _ => null
    };

    protected override string GetStartStep() => Steps.Landing;

    protected override bool IsFinished() => AuthenticationState.IsComplete;

    protected override string GetStepUrl(string step) => step switch
    {
        Steps.Landing => LinkGenerator.TrnTokenLanding(),
        CoreSignInJourney.Steps.Phone => LinkGenerator.RegisterPhone(),
        CoreSignInJourney.Steps.PhoneConfirmation => LinkGenerator.RegisterPhoneConfirmation(),
        _ => throw new ArgumentException($"Unknown step: '{step}'.")
    };

    public new static class Steps
    {
        public const string Landing = $"{nameof(TrnTokenSignInJourney)}.{nameof(Landing)}";
    }
}
