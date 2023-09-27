using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Elevate;

[CheckJourneyType(typeof(ElevateTrnVerificationLevelJourney))]
[CheckCanAccessStep(CurrentStep)]
public class CheckAnswers : PageModel
{
    private const string CurrentStep = ElevateTrnVerificationLevelJourney.Steps.CheckAnswers;

    private readonly ElevateTrnVerificationLevelJourney _journey;

    public CheckAnswers(ElevateTrnVerificationLevelJourney journey)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    public string Trn => _journey.AuthenticationState.StatedTrn!;

    public string? NationalInsuranceNumber => _journey.AuthenticationState.NationalInsuranceNumber;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        await _journey.LookupTrn();
        return await _journey.Advance(CurrentStep);
    }
}
