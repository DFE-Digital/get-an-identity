using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup), typeof(ElevateTrnVerificationLevelJourney))]
[AllowCompletedAuthenticationJourney]
[CheckCanAccessStep(CurrentStep)]
public class EmailExists : PageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.EmailExists;

    private readonly SignInJourney _journey;

    public EmailExists(SignInJourney journey)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    public string? Email => HttpContext.GetAuthenticationState().EmailAddress;

    public async Task<IActionResult> OnPost()
    {
        return await _journey.Advance(CurrentStep);
    }
}
