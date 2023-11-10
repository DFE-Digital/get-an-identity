using System.Diagnostics;

namespace TeacherIdentity.AuthServer.Journeys;

public class ElevateTrnVerificationLevelJourney : SignInJourney
{
    private readonly TrnLookupHelper _trnLookupHelper;

    public ElevateTrnVerificationLevelJourney(
        TrnLookupHelper trnLookupHelper,
        HttpContext httpContext,
        IdentityLinkGenerator linkGenerator,
        UserHelper userHelper) :
        base(httpContext, linkGenerator, userHelper)
    {
        _trnLookupHelper = trnLookupHelper;
    }

    public static string GetStartStepUrl(IdentityLinkGenerator linkGenerator) => linkGenerator.ElevateLanding();

    public async Task LookupTrn()
    {
        var trn = await _trnLookupHelper.LookupTrn(AuthenticationState);
        Debug.Assert(AuthenticationState.TrnVerificationElevationSuccessful.HasValue);

        if (trn is not null)
        {
            Debug.Assert(AuthenticationState.TrnVerificationElevationSuccessful == true);
            await UserHelper.ElevateTrnVerificationLevel(AuthenticationState.UserId!.Value, trn, AuthenticationState.NationalInsuranceNumber!);
        }
        else
        {
            Debug.Assert(AuthenticationState.TrnVerificationElevationSuccessful == false);
            await UserHelper.SetNationalInsuranceNumber(AuthenticationState.UserId!.Value, AuthenticationState.NationalInsuranceNumber!);
        }
    }

    public override bool CanAccessStep(string step) => step switch
    {
        Steps.Landing => true,
        CoreSignInJourneyWithTrnLookup.Steps.NiNumber => true,
        CoreSignInJourneyWithTrnLookup.Steps.Trn => AuthenticationState.HasNationalInsuranceNumber.HasValue,
        Steps.CheckAnswers => AuthenticationState.HasNationalInsuranceNumber == true && AuthenticationState.StatedTrn is not null,
        _ => false
    };

    protected override string? GetNextStep(string currentStep) => currentStep switch
    {
        Steps.Landing => CoreSignInJourneyWithTrnLookup.Steps.NiNumber,
        CoreSignInJourneyWithTrnLookup.Steps.NiNumber => CoreSignInJourneyWithTrnLookup.Steps.Trn,
        CoreSignInJourneyWithTrnLookup.Steps.Trn => Steps.CheckAnswers,
        _ => null
    };

    protected override string? GetPreviousStep(string currentStep) => currentStep switch
    {
        CoreSignInJourneyWithTrnLookup.Steps.NiNumber => Steps.Landing,
        CoreSignInJourneyWithTrnLookup.Steps.Trn => CoreSignInJourneyWithTrnLookup.Steps.NiNumber,
        Steps.CheckAnswers => CoreSignInJourneyWithTrnLookup.Steps.Trn,
        _ => null
    };

    protected override string GetStartStep() => Steps.Landing;

    protected override string GetStepUrl(string step) => step switch
    {
        Steps.Landing => LinkGenerator.ElevateLanding(),
        CoreSignInJourneyWithTrnLookup.Steps.NiNumber => LinkGenerator.RegisterNiNumber(),
        CoreSignInJourneyWithTrnLookup.Steps.Trn => LinkGenerator.RegisterTrn(),
        Steps.CheckAnswers => LinkGenerator.ElevateCheckAnswers(),
        _ => throw new ArgumentException($"Unknown step: '{step}'.")
    };

    // We're done when we've done a lookup, successful or not, using the Strict TrnMatchPolicy
    protected override bool IsFinished() => AuthenticationState.TrnVerificationElevationSuccessful.HasValue;

    public new static class Steps
    {
        public const string Landing = $"{nameof(ElevateTrnVerificationLevelJourney)}.{nameof(Landing)}";
        public const string CheckAnswers = $"{nameof(ElevateTrnVerificationLevelJourney)}.{nameof(CheckAnswers)}";
    }
}
