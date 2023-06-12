using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages;

public class SignOutModel : PageModel
{
    private readonly SignInJourneyProvider _signInJourneyProvider;
    private readonly IClock _clock;

    public SignOutModel(SignInJourneyProvider signInJourneyProvider, IClock clock)
    {
        _signInJourneyProvider = signInJourneyProvider;
        _clock = clock;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        var returnUrl = "/";

        if (HttpContext.TryGetAuthenticationState(out var authenticationState))
        {
            authenticationState.Reset(_clock.UtcNow);

            var signInJourney = _signInJourneyProvider.GetSignInJourney(authenticationState, HttpContext);
            returnUrl = signInJourney.GetStartStepUrl();
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect(returnUrl);
    }
}
