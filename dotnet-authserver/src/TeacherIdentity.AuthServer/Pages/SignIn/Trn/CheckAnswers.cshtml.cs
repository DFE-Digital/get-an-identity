using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[CheckCanAccessStep(CurrentStep)]
public class CheckAnswers : PageModel
{
    private const string CurrentStep = LegacyTrnJourney.Steps.CheckAnswers;

    private readonly LegacyTrnJourney _journey;

    public CheckAnswers(LegacyTrnJourney journey)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep)!;

    public string? EmailAddress => _journey.AuthenticationState.EmailAddress;
    public string? OfficialName => _journey.AuthenticationState.GetOfficialName();
    public string? PreviousOfficialName => _journey.AuthenticationState.GetPreviousOfficialName();
    public string? PreferredName => _journey.AuthenticationState.GetPreferredName();
    public DateOnly? DateOfBirth => _journey.AuthenticationState.DateOfBirth;
    public bool? HaveNationalInsuranceNumber => _journey.AuthenticationState.HasNationalInsuranceNumber;
    public string? NationalInsuranceNumber => _journey.AuthenticationState.NationalInsuranceNumber;
    public bool? AwardedQts => _journey.AuthenticationState.AwardedQts;
    public string? IttProviderName => _journey.AuthenticationState.IttProviderName;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!_journey.FoundATrn)
        {
            return Redirect(_journey.GetNextStepUrl(CurrentStep));
        }

        return await _journey.CreateOrMatchUserWithTrn(currentStep: LegacyTrnJourney.Steps.CheckAnswers);
    }
}
