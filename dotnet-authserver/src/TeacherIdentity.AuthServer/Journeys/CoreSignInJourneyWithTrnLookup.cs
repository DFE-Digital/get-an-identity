using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Journeys;

public class CoreSignInJourneyWithTrnLookup : CoreSignInJourney
{
    private readonly TrnLookupHelper _trnLookupHelper;

    public CoreSignInJourneyWithTrnLookup(
        HttpContext httpContext,
        IdentityLinkGenerator linkGenerator,
        CreateUserHelper createUserHelper, TrnLookupHelper trnLookupHelper)
        : base(httpContext, linkGenerator, createUserHelper)
    {
        _trnLookupHelper = trnLookupHelper;
    }

    public override async Task<IActionResult> TryCreateUser(string currentStep)
    {
        try
        {
            var user = await CreateUserHelper.CreateUserWithTrn(AuthenticationState);

            AuthenticationState.OnTrnLookupCompletedAndUserRegistered(user);
            await AuthenticationState.SignIn(HttpContext);

            if ((!AuthenticationState.TryGetOAuthState(out var oAuthState) || !oAuthState.HasScope(CustomScopes.Trn)) &&
                AuthenticationState.TrnLookupStatus == TrnLookupStatus.Pending)
            {
                await CreateUserHelper.CreateTrnResolutionZendeskTicket(AuthenticationState);
            }
        }
        catch (DbUpdateException dex) when (dex.IsUniqueIndexViolation("ix_users_trn"))
        {
            // We don't currently handle duplicate TRNs in Core Sign In Journey
            return new BadRequestResult();
        }

        return new RedirectResult(GetNextStepUrl(currentStep));
    }

    public bool FoundATrn => AuthenticationState.Trn is not null;

    public override async Task<RedirectResult> Advance(string currentStep)
    {
        if (ShouldPerformTrnLookup(currentStep))
        {
            await _trnLookupHelper.LookupTrn(AuthenticationState);
        }

        return await base.Advance(currentStep);
    }

    private bool ShouldPerformTrnLookup(string step)
    {
        return step == CoreSignInJourney.Steps.DateOfBirth ||
               step == Steps.HasNiNumber && AuthenticationState.HasNationalInsuranceNumber == false ||
               step == Steps.NiNumber ||
               step == Steps.HasTrn && AuthenticationState.HasTrn == false ||
               step == Steps.Trn ||
               step == Steps.HasQts && AuthenticationState.AwardedQts == false ||
               step == Steps.IttProvider;
    }

    protected override bool IsFinished() =>
        AuthenticationState.UserId.HasValue &&
        AuthenticationState.TrnLookupStatus.HasValue &&
        AuthenticationState.TrnLookup == AuthenticationState.TrnLookupState.Complete;

    public override bool CanAccessStep(string step) => step switch
    {
        CoreSignInJourney.Steps.CheckAnswers => AreAllQuestionsAnswered() || FoundATrn,
        Steps.HasNiNumber => AuthenticationState.DateOfBirthSet,
        Steps.NiNumber => AuthenticationState.HasNationalInsuranceNumber == true,
        Steps.HasTrn => AuthenticationState.NationalInsuranceNumberSet || AuthenticationState is { HasNationalInsuranceNumberSet: true, HasNationalInsuranceNumber: false },
        Steps.Trn => AuthenticationState.HasTrn == true,
        Steps.HasQts => AuthenticationState.StatedTrn is not null || AuthenticationState is { HasTrnSet: true, HasTrn: false },
        Steps.IttProvider => AuthenticationState.AwardedQts == true,
        _ => base.CanAccessStep(step)
    };


    protected override string? GetNextStep(string currentStep)
    {
        var shouldCheckAnswers = AreAllQuestionsAnswered() || FoundATrn;

        return (currentStep, AuthenticationState) switch
        {
            (CoreSignInJourney.Steps.DateOfBirth, { ExistingAccountFound: false }) => Steps.HasNiNumber,
            (CoreSignInJourney.Steps.AccountExists, { ExistingAccountChosen: false }) => Steps.HasNiNumber,
            (Steps.HasNiNumber, { HasNationalInsuranceNumber: true }) => Steps.NiNumber,
            (Steps.HasNiNumber, { HasNationalInsuranceNumber: false }) => shouldCheckAnswers ? CoreSignInJourney.Steps.CheckAnswers : Steps.HasTrn,
            (Steps.NiNumber, _) => shouldCheckAnswers ? CoreSignInJourney.Steps.CheckAnswers : Steps.HasTrn,
            (Steps.HasTrn, { HasTrn: true }) => Steps.Trn,
            (Steps.HasTrn, { HasTrn: false }) => shouldCheckAnswers ? CoreSignInJourney.Steps.CheckAnswers : Steps.HasQts,
            (Steps.Trn, _) => shouldCheckAnswers ? CoreSignInJourney.Steps.CheckAnswers : Steps.HasQts,
            (Steps.HasQts, { AwardedQts: true }) => Steps.IttProvider,
            (Steps.HasQts, { AwardedQts: false }) => CoreSignInJourney.Steps.CheckAnswers,
            (Steps.IttProvider, _) => CoreSignInJourney.Steps.CheckAnswers,
            _ => base.GetNextStep(currentStep)
        };
    }

    protected override string? GetPreviousStep(string currentStep) => (currentStep, AuthenticationState) switch
    {
        (Steps.HasNiNumber, _) => CoreSignInJourney.Steps.DateOfBirth,
        (Steps.NiNumber, _) => Steps.HasNiNumber,
        (Steps.HasTrn, { HasNationalInsuranceNumber: true }) => Steps.NiNumber,
        (Steps.HasTrn, { HasNationalInsuranceNumber: false }) => Steps.HasNiNumber,
        (Steps.Trn, _) => Steps.HasTrn,
        (Steps.HasQts, { HasTrn: true }) => Steps.Trn,
        (Steps.HasQts, { HasTrn: false }) => Steps.HasTrn,
        (Steps.IttProvider, _) => Steps.HasQts,
        (CoreSignInJourney.Steps.CheckAnswers, { AwardedQts: true }) => Steps.IttProvider,
        (CoreSignInJourney.Steps.CheckAnswers, { AwardedQts: false }) => Steps.HasQts,
        (CoreSignInJourney.Steps.CheckAnswers, { HasTrn: true }) => Steps.Trn,
        (CoreSignInJourney.Steps.CheckAnswers, { HasTrn: false }) => Steps.HasTrn,
        (CoreSignInJourney.Steps.CheckAnswers, { HasNationalInsuranceNumber: true }) => Steps.NiNumber,
        (CoreSignInJourney.Steps.CheckAnswers, { HasNationalInsuranceNumber: false }) => Steps.HasNiNumber,
        (CoreSignInJourney.Steps.CheckAnswers, { DateOfBirthSet: true }) => CoreSignInJourney.Steps.DateOfBirth,
        (CoreSignInJourney.Steps.CheckAnswers, { OfficialNameSet: true }) => CoreSignInJourney.Steps.Name,
        _ => base.GetPreviousStep(currentStep)
    };

    protected override string GetStepUrl(string step) => step switch
    {
        Steps.HasNiNumber => LinkGenerator.RegisterHasNiNumber(),
        Steps.NiNumber => LinkGenerator.RegisterNiNumber(),
        Steps.HasTrn => LinkGenerator.RegisterHasTrn(),
        Steps.Trn => LinkGenerator.RegisterTrn(),
        Steps.HasQts => LinkGenerator.RegisterHasQts(),
        Steps.IttProvider => LinkGenerator.RegisterIttProvider(),
        _ => base.GetStepUrl(step)
    };

    public new static class Steps
    {
        public const string HasNiNumber = $"{nameof(CoreSignInJourneyWithTrnLookup)}.{nameof(HasNiNumber)}";
        public const string NiNumber = $"{nameof(CoreSignInJourneyWithTrnLookup)}.{nameof(NiNumber)}";
        public const string HasTrn = $"{nameof(CoreSignInJourneyWithTrnLookup)}.{nameof(HasTrn)}";
        public const string Trn = $"{nameof(CoreSignInJourneyWithTrnLookup)}.{nameof(Trn)}";
        public const string HasQts = $"{nameof(CoreSignInJourneyWithTrnLookup)}.{nameof(HasQts)}";
        public const string IttProvider = $"{nameof(CoreSignInJourneyWithTrnLookup)}.{nameof(IttProvider)}";
    }

    protected override bool AreAllQuestionsAnswered() =>
        AuthenticationState.EmailAddressSet &&
        AuthenticationState.EmailAddressVerified &&
        AuthenticationState.MobileNumberSet &&
        AuthenticationState.MobileNumberVerified &&
        AuthenticationState.PreferredNameSet &&
        AuthenticationState.DateOfBirthSet &&
        AuthenticationState.HasNationalInsuranceNumberSet &&
        (AuthenticationState.NationalInsuranceNumberSet || AuthenticationState.HasNationalInsuranceNumber == false) &&
        AuthenticationState.AwardedQtsSet &&
        (AuthenticationState.HasIttProviderSet || AuthenticationState.AwardedQts == false);
}
