using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.BackgroundJobs;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Journeys;

public class CoreSignInJourneyWithTrnLookup : CoreSignInJourney
{
    private readonly TrnLookupHelper _trnLookupHelper;
    private readonly TeacherIdentityApplicationManager _applicationManager;
    private readonly IBackgroundJobScheduler _backgroundJobScheduler;
    private readonly ElevateTrnVerificationLevelJourney _elevateJourney;

    public CoreSignInJourneyWithTrnLookup(
        HttpContext httpContext,
        IdentityLinkGenerator linkGenerator,
        UserHelper userHelper,
        TrnLookupHelper trnLookupHelper,
        TeacherIdentityApplicationManager applicationManager,
        IBackgroundJobScheduler backgroundJobScheduler)
        : base(httpContext, linkGenerator, userHelper)
    {
        _trnLookupHelper = trnLookupHelper;
        _applicationManager = applicationManager;
        _backgroundJobScheduler = backgroundJobScheduler;

        _elevateJourney = new ElevateTrnVerificationLevelJourney(trnLookupHelper, httpContext, linkGenerator, userHelper);
    }

    public override async Task<IActionResult> CreateUser(string currentStep)
    {
        await UserHelper.CheckCanAccessService(AuthenticationState);
        Debug.Assert(AuthenticationState.Blocked.HasValue);

        if (AuthenticationState.Blocked == true)
        {
            var nextPageUrl = GetStepUrl(CoreSignInJourney.Steps.Blocked);
            return new RedirectResult(nextPageUrl);
        }

        using var suppressUniqueIndexViolationScope = SentryErrors.Suppress<DbUpdateException>(ex => ex.IsUniqueIndexViolation(User.TrnUniqueIndexName));
        try
        {
            var user = await UserHelper.CreateUserWithTrnLookup(AuthenticationState);

            AuthenticationState.OnTrnLookupCompletedAndUserRegistered(user);
            await AuthenticationState.SignIn(HttpContext);

            if (AuthenticationState.TrnLookupStatus == TrnLookupStatus.Pending)
            {
                AuthenticationState.EnsureOAuthState();
                var oAuthState = AuthenticationState.OAuthState;

                var client = await _applicationManager.FindByClientIdAsync(oAuthState.ClientId);
                var preferredName = AuthenticationState.GetName();

                if (client!.RaiseTrnResolutionSupportTickets)
                {
                    await _backgroundJobScheduler.Enqueue<UserHelper>(
                        u => u.CreateTrnResolutionZendeskTicket(
                            user.UserId,
                            AuthenticationState.GetName(/*includeMiddleName:*/ true),
                            preferredName,
                            AuthenticationState.EmailAddress,
                            AuthenticationState.DateOfBirth,
                            AuthenticationState.NationalInsuranceNumber,
                            AuthenticationState.IttProviderName,
                            AuthenticationState.StatedTrn,
                            client.DisplayName,
                            AuthenticationState.OAuthState.TrnRequirementType == TrnRequirementType.Required));
                }
            }
        }
        catch (Exception ex) when (suppressUniqueIndexViolationScope.IsExceptionSuppressed(ex))
        {
            return await UserHelper.GeneratePinForExistingUserAccount(this, currentStep);
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
        return (step == CoreSignInJourney.Steps.Email && AuthenticationState.TrnLookup != AuthenticationState.TrnLookupState.None) ||
               step == CoreSignInJourney.Steps.DateOfBirth ||
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

    public override bool IsCompleted()
    {
        var finished = IsFinished();

        if (finished && AuthenticationState.RequiresTrnVerificationLevelElevation == true)
        {
            return false;
        }

        return finished;
    }

    public override bool CanAccessStep(string step) => step switch
    {
        CoreSignInJourney.Steps.CheckAnswers => (AreAllQuestionsAnswered() || FoundATrn) && AuthenticationState.ContactDetailsVerified,
        Steps.HasNiNumber => AuthenticationState is { DateOfBirthSet: true, ContactDetailsVerified: true },
        Steps.NiNumber => AuthenticationState is { HasNationalInsuranceNumber: true, ContactDetailsVerified: true },
        Steps.HasTrn => (AuthenticationState is ({ NationalInsuranceNumberSet: true } or { HasNationalInsuranceNumberSet: true, HasNationalInsuranceNumber: false }) and { ContactDetailsVerified: true }),
        Steps.Trn => AuthenticationState is { HasTrn: true, ContactDetailsVerified: true },
        Steps.HasQts => AuthenticationState is ({ StatedTrn: { }, OAuthState: { TrnMatchPolicy: TrnMatchPolicy.Default } } or { OAuthState: { TrnMatchPolicy: TrnMatchPolicy.Default }, HasTrnSet: true, HasTrn: false }) and { ContactDetailsVerified: true },
        Steps.IttProvider => AuthenticationState is { OAuthState: { TrnMatchPolicy: TrnMatchPolicy.Default }, AwardedQts: true, ContactDetailsVerified: true },
        SignInJourney.Steps.TrnInUse => AuthenticationState.TrnLookup == AuthenticationState.TrnLookupState.ExistingTrnFound,
        SignInJourney.Steps.TrnInUseResendTrnOwnerEmailConfirmation => AuthenticationState.TrnLookup == AuthenticationState.TrnLookupState.ExistingTrnFound,
        SignInJourney.Steps.TrnInUseChooseEmail => AuthenticationState.TrnLookup == AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified,
        _ => base.CanAccessStep(step)
    };

    public override string GetNextStepUrl(string currentStep) =>
        currentStep switch
        {
            ElevateTrnVerificationLevelJourney.Steps.Landing => _elevateJourney.GetStartStepUrl(),
            _ => base.GetNextStepUrl(currentStep)
        };

    protected override string? GetNextStep(string currentStep)
    {
        // If we've signed a user in successfully and the TrnMatchPolicy is Strict
        // but the user's TrnVerificationLevel is Low (or null) we need to switch to the 'elevate' journey
        if (IsFinished() && AuthenticationState.RequiresTrnVerificationLevelElevation == true)
        {
            return _elevateJourney.GetStartStepUrl();
        }

        var shouldCheckAnswers = (AreAllQuestionsAnswered() || FoundATrn) && !AuthenticationState.ExistingAccountFound;

        return (currentStep, AuthenticationState) switch
        {
            (CoreSignInJourney.Steps.DateOfBirth, { ExistingAccountFound: false }) => shouldCheckAnswers ? CoreSignInJourney.Steps.CheckAnswers : Steps.HasNiNumber,
            (CoreSignInJourney.Steps.AccountExists, { ExistingAccountChosen: false }) => Steps.HasNiNumber,
            (Steps.HasNiNumber, { HasNationalInsuranceNumber: true }) => Steps.NiNumber,
            (Steps.HasNiNumber, { HasNationalInsuranceNumber: false }) => shouldCheckAnswers ? CoreSignInJourney.Steps.CheckAnswers : Steps.HasTrn,
            (Steps.NiNumber, _) => shouldCheckAnswers ? CoreSignInJourney.Steps.CheckAnswers : Steps.HasTrn,
            (Steps.HasTrn, { HasTrn: true }) => Steps.Trn,
            (Steps.HasTrn, { HasTrn: false, OAuthState.TrnMatchPolicy: TrnMatchPolicy.Strict }) => CoreSignInJourney.Steps.CheckAnswers,
            (Steps.HasTrn, { HasTrn: false }) => shouldCheckAnswers ? CoreSignInJourney.Steps.CheckAnswers : Steps.HasQts,
            (Steps.Trn, _) => shouldCheckAnswers ? CoreSignInJourney.Steps.CheckAnswers : Steps.HasQts,
            (Steps.HasQts, { AwardedQts: true }) => Steps.IttProvider,
            (Steps.HasQts, { AwardedQts: false }) => CoreSignInJourney.Steps.CheckAnswers,
            (Steps.IttProvider, _) => CoreSignInJourney.Steps.CheckAnswers,
            (CoreSignInJourney.Steps.CheckAnswers, { TrnLookup: AuthenticationState.TrnLookupState.ExistingTrnFound }) => SignInJourney.Steps.TrnInUse,
            (SignInJourney.Steps.TrnInUse, _) => SignInJourney.Steps.TrnInUseChooseEmail,
            (SignInJourney.Steps.TrnInUseResendTrnOwnerEmailConfirmation, _) => SignInJourney.Steps.TrnInUse,
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
        (SignInJourney.Steps.TrnInUseResendTrnOwnerEmailConfirmation, _) => SignInJourney.Steps.TrnInUse,
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
        SignInJourney.Steps.TrnInUse => LinkGenerator.TrnInUse(),
        SignInJourney.Steps.TrnInUseChooseEmail => LinkGenerator.TrnInUseChooseEmail(),
        SignInJourney.Steps.TrnInUseResendTrnOwnerEmailConfirmation => LinkGenerator.ResendTrnOwnerEmailConfirmation(),
        _ => base.GetStepUrl(step)
    };

    // The base implementation cannot deal with 'TRN in use' scenarios since that's not the standard journey
    public override string GetLastAccessibleStepUrl(string? requestedStep) =>
        AuthenticationState.TrnLookup switch
        {
            AuthenticationState.TrnLookupState.ExistingTrnFound => GetStepUrl(SignInJourney.Steps.TrnInUse),
            AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified => GetStepUrl(SignInJourney.Steps.TrnInUseChooseEmail),
            _ => base.GetLastAccessibleStepUrl(requestedStep)
        };

    public override async Task<IActionResult> OnEmailVerified(User? user, string currentStep)
    {
        if (user is not null && user.UserType == Models.UserType.Default && user.TrnLookupStatus is null)
        {
            // User was created in a journey that didn't perform a TRN lookup;
            // we don't have an 'upgrade' story for that scenario yet but we cannot allow the user to sign in.
            throw new NotImplementedException("Cannot lookup a TRN for an existing user.");
        }

        var result = await base.OnEmailVerified(user, currentStep);

        if (user is not null)
        {
            await UserHelper.CheckCanAccessService(AuthenticationState);

            if (AuthenticationState.Blocked == true)
            {
                return new RedirectResult(GetStepUrl(CoreSignInJourney.Steps.Blocked));
            }
        }

        return result;
    }

    protected override bool AreAllQuestionsAnswered() =>
        AuthenticationState.EmailAddressSet &&
        AuthenticationState.EmailAddressVerified &&
        AuthenticationState.MobileNumberVerifiedOrSkipped &&
        AuthenticationState.NameSet &&
        AuthenticationState.DateOfBirthSet &&
        AuthenticationState.HasNationalInsuranceNumberSet &&
        (AuthenticationState.NationalInsuranceNumberSet || AuthenticationState.HasNationalInsuranceNumber == false) &&
        AuthenticationState.HasTrnSet &&
        (AuthenticationState.StatedTrnSet || AuthenticationState.HasTrn == false) &&
        (AuthenticationState.AwardedQtsSet &&
            (AuthenticationState.HasIttProviderSet || AuthenticationState.AwardedQts == false) || (AuthenticationState.AwardedQtsSet == false && AuthenticationState?.OAuthState?.TrnMatchPolicy == TrnMatchPolicy.Strict));

    public new static class Steps
    {
        public const string HasNiNumber = $"{nameof(CoreSignInJourneyWithTrnLookup)}.{nameof(HasNiNumber)}";
        public const string NiNumber = $"{nameof(CoreSignInJourneyWithTrnLookup)}.{nameof(NiNumber)}";
        public const string HasTrn = $"{nameof(CoreSignInJourneyWithTrnLookup)}.{nameof(HasTrn)}";
        public const string Trn = $"{nameof(CoreSignInJourneyWithTrnLookup)}.{nameof(Trn)}";
        public const string HasQts = $"{nameof(CoreSignInJourneyWithTrnLookup)}.{nameof(HasQts)}";
        public const string IttProvider = $"{nameof(CoreSignInJourneyWithTrnLookup)}.{nameof(IttProvider)}";
    }
}
