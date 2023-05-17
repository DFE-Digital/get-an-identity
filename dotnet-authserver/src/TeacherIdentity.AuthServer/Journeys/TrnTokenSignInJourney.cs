using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;

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

    public override async Task<IActionResult> CreateUser(string currentStep)
    {
        try
        {
            var user = await CreateUserHelper.CreateUserWithTrn(AuthenticationState);

            AuthenticationState.OnUserRegistered(user);
            await AuthenticationState.SignIn(HttpContext);
        }
        catch (UniqueConstraintException ex) when (ex.IsUniqueIndexViolation(User.TrnUniqueIndexName))
        {
            // We currently do not handle trn index violations for the trn token sign in journey
        }

        return new RedirectResult(GetNextStepUrl(currentStep));
    }

    public override bool CanAccessStep(string step)
    {
        return step switch
        {
            Steps.Landing => true,
            Steps.CheckAnswers => AuthenticationState.MobileNumberVerified,
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
            (CoreSignInJourney.Steps.PhoneConfirmation, _) => Steps.CheckAnswers,
            (CoreSignInJourney.Steps.ResendPhoneConfirmation, _) => CoreSignInJourney.Steps.PhoneConfirmation,
            _ => null
        };
    }

    protected override string? GetPreviousStep(string currentStep) => (currentStep, AuthenticationState) switch
    {
        (CoreSignInJourney.Steps.Phone, _) => Steps.Landing,
        (CoreSignInJourney.Steps.PhoneConfirmation, _) => CoreSignInJourney.Steps.Phone,
        (CoreSignInJourney.Steps.ResendPhoneConfirmation, _) => CoreSignInJourney.Steps.PhoneConfirmation,
        (Steps.CheckAnswers, { MobileNumberVerified: true }) => CoreSignInJourney.Steps.Phone,
        (Steps.CheckAnswers, { MobileNumberVerified: false }) => CoreSignInJourney.Steps.PhoneConfirmation,
        _ => null
    };

    protected override string GetStartStep() => Steps.Landing;

    protected override bool IsFinished() => AuthenticationState.IsComplete;

    protected override string GetStepUrl(string step) => step switch
    {
        Steps.Landing => LinkGenerator.TrnTokenLanding(),
        Steps.CheckAnswers => LinkGenerator.TrnTokenCheckAnswers(),
        CoreSignInJourney.Steps.Phone => LinkGenerator.RegisterPhone(),
        CoreSignInJourney.Steps.PhoneConfirmation => LinkGenerator.RegisterPhoneConfirmation(),
        CoreSignInJourney.Steps.ResendPhoneConfirmation => LinkGenerator.RegisterResendPhoneConfirmation(),
        _ => throw new ArgumentException($"Unknown step: '{step}'.")
    };

    public new static class Steps
    {
        public const string Landing = $"{nameof(TrnTokenSignInJourney)}.{nameof(Landing)}";
        public const string CheckAnswers = $"{nameof(TrnTokenSignInJourney)}.{nameof(CheckAnswers)}";
    }
}
