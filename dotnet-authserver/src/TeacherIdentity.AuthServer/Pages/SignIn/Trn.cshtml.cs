using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[CheckCanAccessStep(CurrentStep)]
public class TrnModel : PageModel
{
    private const string CurrentStep = LegacyTrnJourney.Steps.Trn;

    private readonly LegacyTrnJourney _journey;

    public TrnModel(LegacyTrnJourney journey)
    {
        _journey = journey;
    }

    public string NextPage => _journey.GetNextStepUrl(CurrentStep);

    public void OnGet()
    {
    }
}
