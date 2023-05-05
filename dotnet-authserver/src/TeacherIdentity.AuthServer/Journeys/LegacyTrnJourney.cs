using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TeacherIdentity.AuthServer.Journeys;

public class LegacyTrnJourney : SignInJourney
{
    private readonly TrnLookupHelper _trnLookupHelper;

    public LegacyTrnJourney(
        CreateUserHelper createUserHelper,
        TrnLookupHelper trnLookupHelper,
        HttpContext httpContext,
        IdentityLinkGenerator linkGenerator)
        : base(httpContext, linkGenerator, createUserHelper)
    {
        Debug.Assert((AuthenticationState.UserRequirements & (UserRequirements.TrnHolder | UserRequirements.DefaultUserType)) != 0);
        _trnLookupHelper = trnLookupHelper;
    }

    public bool FoundATrn => AuthenticationState.Trn is not null;

    public override async Task<IActionResult> CreateUser(string currentStep)
    {
        try
        {
            var user = await CreateUserHelper.CreateUserWithTrn(AuthenticationState);

            AuthenticationState.OnTrnLookupCompletedAndUserRegistered(user);
            await AuthenticationState.SignIn(HttpContext);
        }
        catch (DbUpdateException dex) when (dex.IsUniqueIndexViolation("ix_users_trn"))
        {
            // TRN is already linked to an existing account
            return await CreateUserHelper.GeneratePinForExistingUserAccount(this, currentStep);
        }

        return new RedirectResult(GetNextStepUrl(currentStep));
    }

    public override async Task<RedirectResult> Advance(string currentStep)
    {
        if (ShouldPerformTrnLookup(currentStep))
        {
            var lookupResult = await _trnLookupHelper.LookupTrn(AuthenticationState);
            if (lookupResult is not null)
            {
                return new RedirectResult(GetStepUrl(Steps.CheckAnswers));
            }
        }

        return await base.Advance(currentStep);
    }

    private bool ShouldPerformTrnLookup(string step)
    {
        return step == Steps.OfficialName ||
               step == Steps.DateOfBirth ||
               step == Steps.HasNationalInsuranceNumber && AuthenticationState.HasNationalInsuranceNumber == false ||
               step == Steps.NationalInsuranceNumber ||
               step == Steps.HasTrn ||
               step == Steps.AwardedQts && AuthenticationState.AwardedQts == false ||
               step == Steps.IttProvider;
    }

    protected override bool IsFinished() =>
        AuthenticationState.UserId.HasValue &&
            AuthenticationState.TrnLookupStatus.HasValue &&
            AuthenticationState.TrnLookup == AuthenticationState.TrnLookupState.Complete;

    protected override string GetStartStep() => SignInJourney.Steps.Email;

    protected override string GetStepUrl(string step) => step switch
    {
        SignInJourney.Steps.Email => LinkGenerator.Email(),
        SignInJourney.Steps.EmailConfirmation => LinkGenerator.EmailConfirmation(),
        Steps.Trn => LinkGenerator.Trn(),
        Steps.HasTrn => LinkGenerator.TrnHasTrn(),
        Steps.OfficialName => LinkGenerator.TrnOfficialName(),
        Steps.PreferredName => LinkGenerator.TrnPreferredName(),
        Steps.DateOfBirth => LinkGenerator.TrnDateOfBirth(),
        Steps.HasNationalInsuranceNumber => LinkGenerator.TrnHasNiNumber(),
        Steps.NationalInsuranceNumber => LinkGenerator.TrnNiNumber(),
        Steps.AwardedQts => LinkGenerator.TrnAwardedQts(),
        Steps.IttProvider => LinkGenerator.TrnIttProvider(),
        Steps.CheckAnswers => LinkGenerator.TrnCheckAnswers(),
        Steps.NoMatch => LinkGenerator.TrnNoMatch(),
        SignInJourney.Steps.TrnInUse => LinkGenerator.TrnInUse(),
        SignInJourney.Steps.TrnInUseChooseEmail => LinkGenerator.TrnInUseChooseEmail(),
        SignInJourney.Steps.TrnInUseResendTrnOwnerEmailConfirmation => LinkGenerator.ResendTrnOwnerEmailConfirmation(),
        _ => throw new ArgumentException($"Unknown step: '{step}'.")
    };

    protected override string? GetNextStep(string currentStep) => (currentStep, AuthenticationState) switch
    {
        (SignInJourney.Steps.Email, _) => SignInJourney.Steps.EmailConfirmation,
        (SignInJourney.Steps.EmailConfirmation, _) => Steps.Trn,
        (Steps.Trn, _) => Steps.HasTrn,
        (Steps.HasTrn, _) => Steps.OfficialName,
        (Steps.OfficialName, _) => Steps.PreferredName,
        (Steps.PreferredName, _) => Steps.DateOfBirth,
        (Steps.DateOfBirth, _) => Steps.HasNationalInsuranceNumber,
        (Steps.HasNationalInsuranceNumber, { HasNationalInsuranceNumber: true }) => Steps.NationalInsuranceNumber,
        (Steps.HasNationalInsuranceNumber, { HasNationalInsuranceNumber: false }) => Steps.AwardedQts,
        (Steps.NationalInsuranceNumber, _) => Steps.AwardedQts,
        (Steps.AwardedQts, { AwardedQts: true }) => Steps.IttProvider,
        (Steps.AwardedQts, { AwardedQts: false }) => Steps.CheckAnswers,
        (Steps.IttProvider, _) => Steps.CheckAnswers,
        (Steps.CheckAnswers, { Trn: null }) => Steps.NoMatch,
        (Steps.CheckAnswers, { TrnLookup: AuthenticationState.TrnLookupState.ExistingTrnFound }) => SignInJourney.Steps.TrnInUse,
        (SignInJourney.Steps.TrnInUse, _) => SignInJourney.Steps.TrnInUseChooseEmail,
        (SignInJourney.Steps.TrnInUseResendTrnOwnerEmailConfirmation, _) => SignInJourney.Steps.TrnInUse,
        _ => null
    };

    protected override string? GetPreviousStep(string currentStep) => (currentStep, AuthenticationState) switch
    {
        (SignInJourney.Steps.EmailConfirmation, _) => SignInJourney.Steps.Email,
        (Steps.Trn, _) => SignInJourney.Steps.EmailConfirmation,
        (Steps.HasTrn, _) => Steps.Trn,
        (Steps.OfficialName, _) => Steps.HasTrn,
        (Steps.PreferredName, _) => Steps.OfficialName,
        (Steps.DateOfBirth, _) => Steps.PreferredName,
        (Steps.HasNationalInsuranceNumber, _) => Steps.DateOfBirth,
        (Steps.NationalInsuranceNumber, _) => Steps.HasNationalInsuranceNumber,
        (Steps.AwardedQts, { HasNationalInsuranceNumber: true }) => Steps.NationalInsuranceNumber,
        (Steps.AwardedQts, { HasNationalInsuranceNumber: false }) => Steps.HasNationalInsuranceNumber,
        (Steps.IttProvider, _) => Steps.AwardedQts,
        (Steps.CheckAnswers, { AwardedQts: true }) => Steps.IttProvider,
        (Steps.CheckAnswers, { AwardedQts: false }) => Steps.AwardedQts,
        (Steps.CheckAnswers, { HasNationalInsuranceNumberSet: true }) => Steps.NationalInsuranceNumber,
        (Steps.CheckAnswers, { DateOfBirthSet: true }) => Steps.DateOfBirth,
        (Steps.CheckAnswers, { OfficialNameSet: true }) => Steps.OfficialName,
        (Steps.NoMatch, _) => Steps.CheckAnswers,
        (SignInJourney.Steps.TrnInUseResendTrnOwnerEmailConfirmation, _) => SignInJourney.Steps.TrnInUse,
        _ => null
    };

    public override bool CanAccessStep(string step)
    {
        var allQuestionsAnswered = AreAllQuestionsAnswered();

        return AuthenticationState.TrnLookup switch
        {
            AuthenticationState.TrnLookupState.None => step switch
            {
                SignInJourney.Steps.Email => !AuthenticationState.EmailAddressVerified,
                SignInJourney.Steps.EmailConfirmation => AuthenticationState.EmailAddressSet,
                Steps.Trn => AuthenticationState.EmailAddressVerified,
                Steps.HasTrn => AuthenticationState.EmailAddressVerified,
                Steps.OfficialName => AuthenticationState.HasTrnSet,
                Steps.PreferredName => AuthenticationState.OfficialNameSet,
                Steps.DateOfBirth => AuthenticationState.PreferredNameSet,
                Steps.HasNationalInsuranceNumber => AuthenticationState.DateOfBirthSet,
                Steps.NationalInsuranceNumber =>
                    AuthenticationState.HasNationalInsuranceNumberSet && AuthenticationState.HasNationalInsuranceNumber == true,
                Steps.AwardedQts =>
                    AuthenticationState.HasNationalInsuranceNumber == true && AuthenticationState.NationalInsuranceNumberSet ||
                    AuthenticationState.HasNationalInsuranceNumber == false,
                Steps.IttProvider => AuthenticationState.AwardedQts == true,
                Steps.CheckAnswers => allQuestionsAnswered || FoundATrn,
                Steps.NoMatch => allQuestionsAnswered && !FoundATrn,
                _ => false
            },
            AuthenticationState.TrnLookupState.ExistingTrnFound => step switch
            {
                SignInJourney.Steps.TrnInUse or SignInJourney.Steps.TrnInUseResendTrnOwnerEmailConfirmation => true,
                _ => false,
            },
            AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified => step switch
            {
                SignInJourney.Steps.TrnInUseChooseEmail => true,
                _ => false
            },
            _ => false
        };
    }

    // The base implementation cannot deal with 'TRN in use' scenarios since that's not the standard journey
    public override string GetLastAccessibleStepUrl(string? requestedStep) =>
        AuthenticationState.TrnLookup switch
        {
            AuthenticationState.TrnLookupState.ExistingTrnFound => GetStepUrl(SignInJourney.Steps.TrnInUse),
            AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified => GetStepUrl(SignInJourney.Steps.TrnInUseChooseEmail),
            _ => base.GetLastAccessibleStepUrl(requestedStep)
        };

    private bool AreAllQuestionsAnswered() =>
        AuthenticationState.EmailAddressSet &&
            AuthenticationState.EmailAddressVerified &&
            AuthenticationState.HasTrnSet &&
            AuthenticationState.OfficialNameSet &&
            AuthenticationState.PreferredNameSet &&
            AuthenticationState.DateOfBirthSet &&
            AuthenticationState.HasNationalInsuranceNumberSet &&
            (AuthenticationState.NationalInsuranceNumberSet || AuthenticationState.HasNationalInsuranceNumber == false) &&
            AuthenticationState.AwardedQtsSet &&
            (AuthenticationState.HasIttProviderSet || AuthenticationState.AwardedQts == false);

    public new static class Steps
    {
        public const string Trn = $"{nameof(LegacyTrnJourney)}.{nameof(Trn)}";
        public const string HasTrn = $"{nameof(LegacyTrnJourney)}.{nameof(HasTrn)}";
        public const string OfficialName = $"{nameof(LegacyTrnJourney)}.{nameof(OfficialName)}";
        public const string PreferredName = $"{nameof(LegacyTrnJourney)}.{nameof(PreferredName)}";
        public const string DateOfBirth = $"{nameof(LegacyTrnJourney)}.{nameof(DateOfBirth)}";
        public const string HasNationalInsuranceNumber = $"{nameof(LegacyTrnJourney)}.{nameof(HasNationalInsuranceNumber)}";
        public const string NationalInsuranceNumber = $"{nameof(LegacyTrnJourney)}.{nameof(NationalInsuranceNumber)}";
        public const string AwardedQts = $"{nameof(LegacyTrnJourney)}.{nameof(AwardedQts)}";
        public const string IttProvider = $"{nameof(LegacyTrnJourney)}.{nameof(IttProvider)}";
        public const string CheckAnswers = $"{nameof(LegacyTrnJourney)}.{nameof(CheckAnswers)}";
        public const string NoMatch = $"{nameof(LegacyTrnJourney)}.{nameof(NoMatch)}";
    }
}
