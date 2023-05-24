using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Journeys;

public class TrnTokenSignInJourney : SignInJourney
{
    private readonly TrnTokenHelper _trnTokenHelper;

    public TrnTokenSignInJourney(
        HttpContext httpContext,
        IdentityLinkGenerator linkGenerator,
        CreateUserHelper createUserHelper,
        TrnTokenHelper trnTokenHelper)
        : base(httpContext, linkGenerator, createUserHelper)
    {
        _trnTokenHelper = trnTokenHelper;
    }

    public override async Task<IActionResult> CreateUser(string currentStep)
    {
        try
        {
            var user = await CreateUserHelper.CreateUserWithTrnToken(AuthenticationState);

            AuthenticationState.OnUserRegistered(user);
            await AuthenticationState.SignIn(HttpContext);
        }
        catch (UniqueConstraintException ex) when (ex.IsUniqueIndexViolation(User.TrnUniqueIndexName))
        {
            // We currently do not handle trn index violations for the trn token sign in journey
            throw;
        }

        return new RedirectResult(GetNextStepUrl(currentStep));
    }

    public override async Task<IActionResult> OnEmailVerified(User? user, string currentStep)
    {
        if (user is not null && user.UserType != UserType.Staff)
        {
            await _trnTokenHelper.ApplyTrnTokenToUser(user.UserId, AuthenticationState.TrnToken!);
        }

        return await base.OnEmailVerified(user, currentStep);
    }

    public override bool CanAccessStep(string step)
    {
        return step switch
        {
            Steps.Landing => true,
            Steps.CheckAnswers => AuthenticationState.ContactDetailsVerified,
            SignInJourney.Steps.Email => true,
            SignInJourney.Steps.EmailConfirmation => AuthenticationState is { EmailAddressSet: true, EmailAddressVerified: false },
            CoreSignInJourney.Steps.DateOfBirth => AuthenticationState.ContactDetailsVerified,
            CoreSignInJourney.Steps.Phone => true,
            CoreSignInJourney.Steps.PhoneConfirmation => AuthenticationState is { MobileNumberSet: true, MobileNumberVerified: false },
            CoreSignInJourney.Steps.ResendPhoneConfirmation => AuthenticationState is { MobileNumberSet: true, MobileNumberVerified: false },
            CoreSignInJourney.Steps.Email => AuthenticationState.MobileNumberVerified,
            CoreSignInJourney.Steps.EmailConfirmation => AuthenticationState is { EmailAddressSet: true, EmailAddressVerified: false, MobileNumberVerified: true },
            CoreSignInJourney.Steps.ResendEmailConfirmation => AuthenticationState is { EmailAddressSet: true, EmailAddressVerified: false },
            CoreSignInJourney.Steps.AccountExists => AuthenticationState.ExistingAccountFound,
            CoreSignInJourney.Steps.ExistingAccountEmailConfirmation => AuthenticationState is { ExistingAccountFound: true, ExistingAccountChosen: true },
            CoreSignInJourney.Steps.ResendExistingAccountEmail => AuthenticationState is { ExistingAccountFound: true, ExistingAccountChosen: true },
            CoreSignInJourney.Steps.ExistingAccountPhone => AuthenticationState is { ExistingAccountFound: true, ExistingAccountChosen: true, ExistingAccountMobileNumber: { } },
            CoreSignInJourney.Steps.ExistingAccountPhoneConfirmation => AuthenticationState is { ExistingAccountFound: true, ExistingAccountChosen: true, ExistingAccountMobileNumber: { } },
            CoreSignInJourney.Steps.ResendExistingAccountPhone => AuthenticationState is { ExistingAccountFound: true, ExistingAccountChosen: true, ExistingAccountMobileNumber: { } },
            _ => false
        };
    }

    protected override string? GetNextStep(string currentStep)
    {
        return (currentStep, AuthenticationState) switch
        {
            (Steps.Landing, { ExistingAccountFound: true }) => CoreSignInJourney.Steps.AccountExists,
            (Steps.Landing, { ExistingAccountFound: false }) => CoreSignInJourney.Steps.Phone,
            (SignInJourney.Steps.Email, _) => SignInJourney.Steps.EmailConfirmation,
            (CoreSignInJourney.Steps.Phone, _) => CoreSignInJourney.Steps.PhoneConfirmation,
            (CoreSignInJourney.Steps.PhoneConfirmation, _) => Steps.CheckAnswers,
            (CoreSignInJourney.Steps.Email, _) => CoreSignInJourney.Steps.EmailConfirmation,
            (CoreSignInJourney.Steps.EmailConfirmation, _) => Steps.CheckAnswers,
            (CoreSignInJourney.Steps.DateOfBirth, _) => Steps.CheckAnswers,
            (CoreSignInJourney.Steps.ResendPhoneConfirmation, _) => CoreSignInJourney.Steps.PhoneConfirmation,
            (CoreSignInJourney.Steps.AccountExists, { ExistingAccountChosen: true }) => CoreSignInJourney.Steps.ExistingAccountEmailConfirmation,
            (CoreSignInJourney.Steps.AccountExists, { ExistingAccountChosen: false }) => CoreSignInJourney.Steps.Phone,
            (CoreSignInJourney.Steps.ExistingAccountEmailConfirmation, _) => CoreSignInJourney.Steps.ExistingAccountPhone,
            (CoreSignInJourney.Steps.ResendExistingAccountEmail, _) => CoreSignInJourney.Steps.ExistingAccountEmailConfirmation,
            (CoreSignInJourney.Steps.ExistingAccountPhone, _) => CoreSignInJourney.Steps.ExistingAccountPhoneConfirmation,
            (CoreSignInJourney.Steps.ResendExistingAccountPhone, _) => CoreSignInJourney.Steps.ExistingAccountPhoneConfirmation,
            _ => null
        };
    }

    protected override string? GetPreviousStep(string currentStep) => (currentStep, AuthenticationState) switch
    {
        (SignInJourney.Steps.Email, _) => Steps.Landing,
        (SignInJourney.Steps.EmailConfirmation, _) => SignInJourney.Steps.Email,
        (CoreSignInJourney.Steps.Phone, _) => Steps.Landing,
        (CoreSignInJourney.Steps.PhoneConfirmation, _) => CoreSignInJourney.Steps.Phone,
        (CoreSignInJourney.Steps.ResendPhoneConfirmation, _) => CoreSignInJourney.Steps.PhoneConfirmation,
        (CoreSignInJourney.Steps.Email, { ContactDetailsVerified: true }) => Steps.CheckAnswers,
        (CoreSignInJourney.Steps.Email, { MobileNumberVerified: false }) => CoreSignInJourney.Steps.PhoneConfirmation,
        (CoreSignInJourney.Steps.Email, { MobileNumberVerified: true }) => CoreSignInJourney.Steps.Email,
        (CoreSignInJourney.Steps.EmailConfirmation, _) => CoreSignInJourney.Steps.Email,
        (CoreSignInJourney.Steps.ResendEmailConfirmation, _) => CoreSignInJourney.Steps.EmailConfirmation,
        (CoreSignInJourney.Steps.DateOfBirth, _) => Steps.CheckAnswers,
        (Steps.CheckAnswers, { EmailAddressVerified: false }) => CoreSignInJourney.Steps.EmailConfirmation,
        (Steps.CheckAnswers, { MobileNumberVerified: false }) => CoreSignInJourney.Steps.PhoneConfirmation,
        (Steps.CheckAnswers, _) => CoreSignInJourney.Steps.Phone,
        (CoreSignInJourney.Steps.AccountExists, _) => Steps.Landing,
        (CoreSignInJourney.Steps.ExistingAccountEmailConfirmation, _) => CoreSignInJourney.Steps.AccountExists,
        (CoreSignInJourney.Steps.ResendExistingAccountEmail, _) => CoreSignInJourney.Steps.ExistingAccountEmailConfirmation,
        (CoreSignInJourney.Steps.ExistingAccountPhone, _) => CoreSignInJourney.Steps.ExistingAccountEmailConfirmation,
        (CoreSignInJourney.Steps.ExistingAccountPhoneConfirmation, _) => CoreSignInJourney.Steps.ExistingAccountPhone,
        (CoreSignInJourney.Steps.ResendExistingAccountPhone, _) => CoreSignInJourney.Steps.ExistingAccountPhoneConfirmation,
        _ => null
    };

    protected override string GetStartStep() => Steps.Landing;

    protected override bool IsFinished() => AuthenticationState.IsComplete;

    protected override string GetStepUrl(string step) => step switch
    {
        Steps.Landing => LinkGenerator.TrnTokenLanding(),
        Steps.CheckAnswers => LinkGenerator.TrnTokenCheckAnswers(),
        SignInJourney.Steps.Email => LinkGenerator.Email(),
        SignInJourney.Steps.EmailConfirmation => LinkGenerator.EmailConfirmation(),
        CoreSignInJourney.Steps.Phone => LinkGenerator.RegisterPhone(),
        CoreSignInJourney.Steps.PhoneConfirmation => LinkGenerator.RegisterPhoneConfirmation(),
        CoreSignInJourney.Steps.ResendPhoneConfirmation => LinkGenerator.RegisterResendPhoneConfirmation(),
        CoreSignInJourney.Steps.Email => LinkGenerator.RegisterEmail(),
        CoreSignInJourney.Steps.EmailConfirmation => LinkGenerator.RegisterEmailConfirmation(),
        CoreSignInJourney.Steps.ResendEmailConfirmation => LinkGenerator.RegisterResendEmailConfirmation(),
        CoreSignInJourney.Steps.DateOfBirth => LinkGenerator.RegisterDateOfBirth(),
        CoreSignInJourney.Steps.AccountExists => LinkGenerator.RegisterAccountExists(),
        CoreSignInJourney.Steps.ExistingAccountEmailConfirmation => LinkGenerator.RegisterExistingAccountEmailConfirmation(),
        CoreSignInJourney.Steps.ExistingAccountPhone => LinkGenerator.RegisterExistingAccountPhone(),
        CoreSignInJourney.Steps.ExistingAccountPhoneConfirmation => LinkGenerator.RegisterExistingAccountPhoneConfirmation(),
        CoreSignInJourney.Steps.ResendExistingAccountEmail => LinkGenerator.RegisterResendExistingAccountEmail(),
        CoreSignInJourney.Steps.ResendExistingAccountPhone => LinkGenerator.RegisterResendExistingAccountPhone(),
        _ => throw new ArgumentException($"Unknown step: '{step}'.")
    };

    public new static class Steps
    {
        public const string Landing = $"{nameof(TrnTokenSignInJourney)}.{nameof(Landing)}";
        public const string CheckAnswers = $"{nameof(TrnTokenSignInJourney)}.{nameof(CheckAnswers)}";
    }
}
