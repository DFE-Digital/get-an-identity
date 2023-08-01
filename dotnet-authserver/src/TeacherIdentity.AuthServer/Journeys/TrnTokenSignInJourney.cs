using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Journeys;

public class TrnTokenSignInJourney : SignInJourney
{
    private readonly TrnTokenHelper _trnTokenHelper;

    public TrnTokenSignInJourney(
        HttpContext httpContext,
        IdentityLinkGenerator linkGenerator,
        UserHelper userHelper, TrnTokenHelper trnTokenHelper)
        : base(httpContext, linkGenerator, userHelper)
    {
        _trnTokenHelper = trnTokenHelper;
    }

    public override async Task<IActionResult> CreateUser(string currentStep)
    {
        var user = await UserHelper.CreateUserWithTrnToken(AuthenticationState);

        AuthenticationState.OnUserRegistered(user);
        await AuthenticationState.SignIn(HttpContext);

        return new RedirectResult(GetNextStepUrl(currentStep));
    }

    public override string GetNextStepUrl(string currentStep)
    {
        if (IsFinished())
        {
            return currentStep switch
            {
                CoreSignInJourney.Steps.PhoneConfirmation => GetStepUrl(CoreSignInJourney.Steps.PhoneExists),
                _ => AuthenticationState.PostSignInUrl
            };
        }

        return base.GetNextStepUrl(currentStep);
    }

    public override async Task<IActionResult> OnEmailVerified(User? user, string currentStep)
    {
        if (user is not null && AuthenticationState.HasTrnToken)
        {
            await _trnTokenHelper.ApplyTrnTokenToUser(user.UserId, AuthenticationState.TrnToken!);
        }

        return await base.OnEmailVerified(user, currentStep);
    }

    public override async Task<IActionResult> OnMobileVerified(User? user, string currentStep)
    {
        if (user is not null && AuthenticationState.HasTrnToken)
        {
            await _trnTokenHelper.ApplyTrnTokenToUser(user.UserId, AuthenticationState.TrnToken!);
        }

        return await base.OnMobileVerified(user, currentStep);
    }

    public override bool CanAccessStep(string step)
    {
        return step switch
        {
            Steps.Landing => true,
            Steps.CheckAnswers => AuthenticationState.ContactDetailsVerified,
            SignInJourney.Steps.Email => true,
            SignInJourney.Steps.EmailConfirmation => AuthenticationState is { EmailAddressSet: true, EmailAddressVerified: false },
            CoreSignInJourney.Steps.Phone => AuthenticationState.EmailAddressVerified,
            CoreSignInJourney.Steps.PhoneConfirmation => AuthenticationState is { MobileNumberSet: true, MobileNumberVerified: false, EmailAddressVerified: true },
            CoreSignInJourney.Steps.PhoneExists => AuthenticationState.IsComplete,
            CoreSignInJourney.Steps.ResendPhoneConfirmation => AuthenticationState is { MobileNumberSet: true, MobileNumberVerified: false },
            CoreSignInJourney.Steps.Email => AuthenticationState.MobileNumberVerified,
            CoreSignInJourney.Steps.EmailConfirmation => AuthenticationState is { EmailAddressSet: true, EmailAddressVerified: false, MobileNumberVerified: true },
            CoreSignInJourney.Steps.ResendEmailConfirmation => AuthenticationState is { EmailAddressSet: true, EmailAddressVerified: false },
            CoreSignInJourney.Steps.InstitutionEmail => AuthenticationState is { EmailAddressSet: true, EmailAddressVerified: true, MobileNumberVerified: true, IsInstitutionEmail: true },
            CoreSignInJourney.Steps.PreferredName => AuthenticationState.ContactDetailsVerified,
            CoreSignInJourney.Steps.DateOfBirth => AuthenticationState is { PreferredNameSet: true, ContactDetailsVerified: true },
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
        var shouldCheckAnswers = AreAllQuestionsAnswered() && !AuthenticationState.ExistingAccountFound;

        return (currentStep, AuthenticationState) switch
        {
            (Steps.Landing, { ExistingAccountFound: true }) => CoreSignInJourney.Steps.AccountExists,
            (Steps.Landing, { ExistingAccountFound: false }) => CoreSignInJourney.Steps.Phone,
            (SignInJourney.Steps.Email, _) => SignInJourney.Steps.EmailConfirmation,
            (SignInJourney.Steps.EmailConfirmation, { IsComplete: true }) => CoreSignInJourney.Steps.EmailExists,
            (SignInJourney.Steps.EmailConfirmation, { IsComplete: false }) => shouldCheckAnswers ? Steps.CheckAnswers : CoreSignInJourney.Steps.Phone,
            (CoreSignInJourney.Steps.Phone, _) => CoreSignInJourney.Steps.PhoneConfirmation,
            (CoreSignInJourney.Steps.PhoneConfirmation, { IsComplete: true }) => CoreSignInJourney.Steps.PhoneExists,
            (CoreSignInJourney.Steps.PhoneConfirmation, { IsComplete: false }) => shouldCheckAnswers ? Steps.CheckAnswers : CoreSignInJourney.Steps.PreferredName,
            (CoreSignInJourney.Steps.Email, _) => CoreSignInJourney.Steps.EmailConfirmation,
            (CoreSignInJourney.Steps.EmailConfirmation, { IsInstitutionEmail: false }) => Steps.CheckAnswers,
            (CoreSignInJourney.Steps.EmailConfirmation, { IsInstitutionEmail: true }) => CoreSignInJourney.Steps.InstitutionEmail,
            (CoreSignInJourney.Steps.ResendEmailConfirmation, _) => CoreSignInJourney.Steps.EmailConfirmation,
            (CoreSignInJourney.Steps.InstitutionEmail, { InstitutionEmailChosen: true }) => Steps.CheckAnswers,
            (CoreSignInJourney.Steps.InstitutionEmail, _) => CoreSignInJourney.Steps.EmailConfirmation,
            (CoreSignInJourney.Steps.PreferredName, _) => Steps.CheckAnswers,
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
        (CoreSignInJourney.Steps.PhoneExists, { MobileNumberVerified: true }) => CoreSignInJourney.Steps.Phone,
        (CoreSignInJourney.Steps.PhoneExists, { MobileNumberVerified: false }) => CoreSignInJourney.Steps.PhoneConfirmation,
        (CoreSignInJourney.Steps.Email, { ContactDetailsVerified: true }) => Steps.CheckAnswers,
        (CoreSignInJourney.Steps.Email, { MobileNumberVerified: false }) => CoreSignInJourney.Steps.PhoneConfirmation,
        (CoreSignInJourney.Steps.Email, { MobileNumberVerified: true }) => CoreSignInJourney.Steps.Email,
        (CoreSignInJourney.Steps.EmailConfirmation, _) => CoreSignInJourney.Steps.Email,
        (CoreSignInJourney.Steps.ResendEmailConfirmation, _) => CoreSignInJourney.Steps.EmailConfirmation,
        (CoreSignInJourney.Steps.InstitutionEmail, { EmailAddressVerified: false }) => CoreSignInJourney.Steps.EmailConfirmation,
        (CoreSignInJourney.Steps.InstitutionEmail, { EmailAddressVerified: true }) => CoreSignInJourney.Steps.Email,
        (CoreSignInJourney.Steps.PreferredName, { MobileNumberVerified: true }) => CoreSignInJourney.Steps.Phone,
        (CoreSignInJourney.Steps.PreferredName, { MobileNumberVerified: false }) => CoreSignInJourney.Steps.PhoneConfirmation,
        (CoreSignInJourney.Steps.DateOfBirth, _) => Steps.CheckAnswers,
        (Steps.CheckAnswers, { EmailAddressVerified: false }) => CoreSignInJourney.Steps.EmailConfirmation,
        (Steps.CheckAnswers, { EmailAddressVerified: true, IsInstitutionEmail: true, InstitutionEmailChosen: false }) => CoreSignInJourney.Steps.InstitutionEmail,
        (Steps.CheckAnswers, { EmailAddressVerified: true, IsInstitutionEmail: true, InstitutionEmailChosen: null }) => CoreSignInJourney.Steps.InstitutionEmail,
        (Steps.CheckAnswers, { MobileNumberVerified: false }) => CoreSignInJourney.Steps.PreferredName,
        (Steps.CheckAnswers, _) => CoreSignInJourney.Steps.Phone,
        (CoreSignInJourney.Steps.AccountExists, _) => Steps.Landing,
        (CoreSignInJourney.Steps.ExistingAccountEmailConfirmation, _) => CoreSignInJourney.Steps.AccountExists,
        (CoreSignInJourney.Steps.ResendExistingAccountEmail, _) => CoreSignInJourney.Steps.ExistingAccountEmailConfirmation,
        (CoreSignInJourney.Steps.ExistingAccountPhone, _) => CoreSignInJourney.Steps.ExistingAccountEmailConfirmation,
        (CoreSignInJourney.Steps.ExistingAccountPhoneConfirmation, _) => CoreSignInJourney.Steps.ExistingAccountPhone,
        (CoreSignInJourney.Steps.ResendExistingAccountPhone, _) => CoreSignInJourney.Steps.ExistingAccountPhoneConfirmation,
        _ => null
    };

    private bool AreAllQuestionsAnswered() =>
        AuthenticationState is
        {
            EmailAddressSet: true,
            EmailAddressVerified: true,
            HasValidEmail: true,
            MobileNumberSet: true,
            MobileNumberVerified: true,
            NameSet: true,
            PreferredNameSet: true,
            DateOfBirthSet: true,
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
        CoreSignInJourney.Steps.PhoneExists => LinkGenerator.RegisterPhoneExists(),
        CoreSignInJourney.Steps.ResendPhoneConfirmation => LinkGenerator.RegisterResendPhoneConfirmation(),
        CoreSignInJourney.Steps.Email => LinkGenerator.RegisterEmail(),
        CoreSignInJourney.Steps.EmailConfirmation => LinkGenerator.RegisterEmailConfirmation(),
        CoreSignInJourney.Steps.InstitutionEmail => LinkGenerator.RegisterInstitutionEmail(),
        CoreSignInJourney.Steps.ResendEmailConfirmation => LinkGenerator.RegisterResendEmailConfirmation(),
        CoreSignInJourney.Steps.PreferredName => LinkGenerator.RegisterPreferredName(),
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
