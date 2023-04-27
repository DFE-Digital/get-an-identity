namespace TeacherIdentity.AuthServer.Journeys;

public class CoreSignInJourney : SignInJourney
{
    public CoreSignInJourney(
        HttpContext httpContext,
        IdentityLinkGenerator linkGenerator,
        CreateUserHelper createUserHelper)
        : base(httpContext, linkGenerator, createUserHelper)
    {
    }

    public override string GetNextStepUrl(string currentStep)
    {
        if (IsFinished())
        {
            return currentStep switch
            {
                Steps.EmailConfirmation => GetStepUrl(Steps.EmailExists),
                Steps.PhoneConfirmation => GetStepUrl(Steps.PhoneExists),
                _ => AuthenticationState.PostSignInUrl
            };
        }

        return base.GetNextStepUrl(currentStep);
    }

    public override bool CanAccessStep(string step)
    {
        return step switch
        {
            Steps.Email => !AuthenticationState.EmailAddressVerified,
            Steps.EmailConfirmation => AuthenticationState.EmailAddressSet,
            Steps.ResendEmailConfirmation => AuthenticationState is { EmailAddressSet: true, EmailAddressVerified: false },
            Steps.EmailExists => AuthenticationState.IsComplete,
            Steps.Phone => AuthenticationState.EmailAddressVerified,
            Steps.PhoneConfirmation => AuthenticationState.MobileNumberSet,
            Steps.ResendPhoneConfirmation => AuthenticationState is { MobileNumberSet: true, MobileNumberVerified: false },
            Steps.PhoneExists => AuthenticationState.IsComplete,
            Steps.Name => AuthenticationState.MobileNumberVerified,
            Steps.DateOfBirth => AuthenticationState.PreferredNameSet,
            Steps.AccountExists => AuthenticationState.ExistingAccountFound,
            Steps.ExistingAccountEmailConfirmation => AuthenticationState is { ExistingAccountFound: true, ExistingAccountChosen: true },
            Steps.ResendExistingAccountEmail => AuthenticationState is { ExistingAccountFound: true, ExistingAccountChosen: true },
            Steps.ExistingAccountPhone => AuthenticationState is { ExistingAccountFound: true, ExistingAccountChosen: true, ExistingAccountMobileNumber: { } },
            Steps.ExistingAccountPhoneConfirmation => AuthenticationState is { ExistingAccountFound: true, ExistingAccountChosen: true, ExistingAccountMobileNumber: { } },
            Steps.ResendExistingAccountPhone => AuthenticationState is { ExistingAccountFound: true, ExistingAccountChosen: true, ExistingAccountMobileNumber: { } },
            Steps.ChangeEmailRequest => AuthenticationState is { ExistingAccountFound: true, ExistingAccountChosen: false },
            Steps.CheckAnswers => AreAllQuestionsAnswered(),
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, null)
        };
    }

    protected override string? GetNextStep(string currentStep)
    {
        var shouldCheckAnswers = AreAllQuestionsAnswered();

        return (currentStep, AuthenticationState) switch
        {
            (Steps.Email, _) => Steps.EmailConfirmation,
            (Steps.EmailConfirmation, { IsComplete: true }) => Steps.EmailExists,
            (Steps.EmailConfirmation, { IsComplete: false }) => shouldCheckAnswers ? Steps.CheckAnswers : Steps.Phone,
            (Steps.ResendEmailConfirmation, _) => Steps.EmailConfirmation,
            (Steps.Phone, _) => Steps.PhoneConfirmation,
            (Steps.PhoneConfirmation, { IsComplete: true }) => Steps.PhoneExists,
            (Steps.PhoneConfirmation, { IsComplete: false }) => shouldCheckAnswers ? Steps.CheckAnswers : Steps.Name,
            (Steps.ResendPhoneConfirmation, _) => Steps.PhoneConfirmation,
            (Steps.Name, _) => shouldCheckAnswers ? Steps.CheckAnswers : Steps.DateOfBirth,
            (Steps.DateOfBirth, { ExistingAccountFound: true }) => Steps.AccountExists,
            (Steps.DateOfBirth, { ExistingAccountFound: false }) => Steps.CheckAnswers,
            (Steps.AccountExists, { ExistingAccountChosen: true }) => Steps.ExistingAccountEmailConfirmation,
            (Steps.AccountExists, { ExistingAccountChosen: false }) => Steps.CheckAnswers,
            (Steps.ExistingAccountEmailConfirmation, _) => Steps.ExistingAccountPhone,
            (Steps.ResendExistingAccountEmail, _) => Steps.ExistingAccountEmailConfirmation,
            (Steps.ExistingAccountPhone, _) => Steps.ExistingAccountPhoneConfirmation,
            (Steps.ResendExistingAccountPhone, _) => Steps.ExistingAccountPhoneConfirmation,
            _ => null
        };
    }

    protected override string? GetPreviousStep(string currentStep) => (currentStep, AuthenticationState) switch
    {
        (Steps.EmailConfirmation, _) => Steps.Email,
        (Steps.ResendEmailConfirmation, _) => Steps.EmailConfirmation,
        (Steps.EmailExists, _) => Steps.EmailConfirmation,
        (Steps.Phone, _) => Steps.EmailConfirmation,
        (Steps.PhoneConfirmation, _) => Steps.Phone,
        (Steps.ResendPhoneConfirmation, _) => Steps.PhoneConfirmation,
        (Steps.PhoneExists, _) => Steps.PhoneConfirmation,
        (Steps.Name, _) => Steps.PhoneConfirmation,
        (Steps.DateOfBirth, _) => Steps.Name,
        (Steps.AccountExists, _) => Steps.DateOfBirth,
        (Steps.ExistingAccountEmailConfirmation, _) => Steps.AccountExists,
        (Steps.ResendExistingAccountEmail, _) => Steps.ExistingAccountEmailConfirmation,
        (Steps.ExistingAccountPhone, _) => Steps.ExistingAccountEmailConfirmation,
        (Steps.ExistingAccountPhoneConfirmation, _) => Steps.ExistingAccountPhone,
        (Steps.ResendExistingAccountPhone, _) => Steps.ExistingAccountPhoneConfirmation,
        (Steps.CheckAnswers, _) => Steps.DateOfBirth,
        _ => null
    };

    protected override string GetStartStep() => Steps.Email;

    protected override bool IsFinished() => AuthenticationState.IsComplete;

    protected override string GetStepUrl(string step) => step switch
    {
        Steps.Email => LinkGenerator.RegisterEmail(),
        Steps.EmailConfirmation => LinkGenerator.RegisterEmailConfirmation(),
        Steps.ResendEmailConfirmation => LinkGenerator.RegisterResendEmailConfirmation(),
        Steps.EmailExists => LinkGenerator.RegisterEmailExists(),
        Steps.Phone => LinkGenerator.RegisterPhone(),
        Steps.PhoneConfirmation => LinkGenerator.RegisterPhoneConfirmation(),
        Steps.ResendPhoneConfirmation => LinkGenerator.RegisterResendPhoneConfirmation(),
        Steps.PhoneExists => LinkGenerator.RegisterPhoneExists(),
        Steps.Name => LinkGenerator.RegisterName(),
        Steps.DateOfBirth => LinkGenerator.RegisterDateOfBirth(),
        Steps.AccountExists => LinkGenerator.RegisterAccountExists(),
        Steps.ExistingAccountEmailConfirmation => LinkGenerator.RegisterExistingAccountEmailConfirmation(),
        Steps.ResendExistingAccountEmail => LinkGenerator.RegisterResendExistingAccountEmail(),
        Steps.ExistingAccountPhone => LinkGenerator.RegisterExistingAccountPhone(),
        Steps.ExistingAccountPhoneConfirmation => LinkGenerator.RegisterExistingAccountPhoneConfirmation(),
        Steps.ResendExistingAccountPhone => LinkGenerator.RegisterResendExistingAccountPhone(),
        Steps.ChangeEmailRequest => LinkGenerator.RegisterChangeEmailRequest(),
        Steps.CheckAnswers => LinkGenerator.RegisterCheckAnswers(),
        _ => throw new ArgumentException($"Unknown step: '{step}'.")
    };

    public new static class Steps
    {
        public const string Email = $"{nameof(CoreSignInJourney)}.{nameof(Email)}";
        public const string EmailConfirmation = $"{nameof(CoreSignInJourney)}.{nameof(EmailConfirmation)}";
        public const string ResendEmailConfirmation = $"{nameof(CoreSignInJourney)}.{nameof(ResendEmailConfirmation)}";
        public const string EmailExists = $"{nameof(CoreSignInJourney)}.{nameof(EmailExists)}";
        public const string Phone = $"{nameof(CoreSignInJourney)}.{nameof(Phone)}";
        public const string PhoneConfirmation = $"{nameof(CoreSignInJourney)}.{nameof(PhoneConfirmation)}";
        public const string ResendPhoneConfirmation = $"{nameof(CoreSignInJourney)}.{nameof(ResendPhoneConfirmation)}";
        public const string PhoneExists = $"{nameof(CoreSignInJourney)}.{nameof(PhoneExists)}";
        public const string Name = $"{nameof(CoreSignInJourney)}.{nameof(Name)}";
        public const string DateOfBirth = $"{nameof(CoreSignInJourney)}.{nameof(DateOfBirth)}";
        public const string AccountExists = $"{nameof(CoreSignInJourney)}.{nameof(AccountExists)}";
        public const string ExistingAccountEmailConfirmation = $"{nameof(CoreSignInJourney)}.{nameof(ExistingAccountEmailConfirmation)}";
        public const string ResendExistingAccountEmail = $"{nameof(CoreSignInJourney)}.{nameof(ResendExistingAccountEmail)}";
        public const string ExistingAccountPhone = $"{nameof(CoreSignInJourney)}.{nameof(ExistingAccountPhone)}";
        public const string ExistingAccountPhoneConfirmation = $"{nameof(CoreSignInJourney)}.{nameof(ExistingAccountPhoneConfirmation)}";
        public const string ResendExistingAccountPhone = $"{nameof(CoreSignInJourney)}.{nameof(ResendExistingAccountPhone)}";
        public const string ChangeEmailRequest = $"{nameof(CoreSignInJourney)}.{nameof(ChangeEmailRequest)}";
        public const string CheckAnswers = $"{nameof(CoreSignInJourney)}.{nameof(CheckAnswers)}";
    }

    protected virtual bool AreAllQuestionsAnswered() =>
        AuthenticationState.EmailAddressSet &&
        AuthenticationState.EmailAddressVerified &&
        AuthenticationState.MobileNumberSet &&
        AuthenticationState.MobileNumberVerified &&
        AuthenticationState.PreferredNameSet &&
        AuthenticationState.DateOfBirthSet;
}
