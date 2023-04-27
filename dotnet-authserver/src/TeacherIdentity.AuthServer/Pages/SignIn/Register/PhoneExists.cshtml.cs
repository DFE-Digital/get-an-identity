using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[AllowCompletedAuthenticationJourney]
[CheckCanAccessStep(CurrentStep)]
public class PhoneExists : PageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.PhoneExists;

    private SignInJourney _journey;

    public PhoneExists(SignInJourney journey)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    public string? MobileNumber => HttpContext.GetAuthenticationState().MobileNumber;

    public async Task<IActionResult> OnPost()
    {
        return await _journey.Advance(CurrentStep);
    }
}
