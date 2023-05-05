using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[AllowExpiredAuthenticationJourney, AllowCompletedAuthenticationJourney]
public class ResetModel : PageModel
{
    private readonly SignInJourney _journey;
    private readonly IClock _clock;

    public ResetModel(SignInJourney journey, IClock clock)
    {
        _journey = journey;
        _clock = clock;
    }

    public IActionResult OnPost()
    {
        _journey.AuthenticationState.Reset(_clock.UtcNow);
        return Redirect(_journey.GetStartStepUrl());
    }
}
