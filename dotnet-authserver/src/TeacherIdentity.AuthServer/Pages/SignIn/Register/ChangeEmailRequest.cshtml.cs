using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckCanAccessStep(CurrentStep)]
public class ChangeEmailRequest : PageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.ChangeEmailRequest;

    private readonly SignInJourney _journey;

    public ChangeEmailRequest(SignInJourney journey)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);
}
