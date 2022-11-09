using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[AllowExpiredAuthenticationJourney, AllowCompletedAuthenticationJourney]
public class ResetModel : PageModel
{
    private readonly IClock _clock;
    private readonly IIdentityLinkGenerator _linkGenerator;

    public ResetModel(IClock clock, IIdentityLinkGenerator linkGenerator)
    {
        _clock = clock;
        _linkGenerator = linkGenerator;
    }

    public IActionResult OnPost()
    {
        var authenticationState = HttpContext.GetAuthenticationState();
        authenticationState.Reset(_clock.UtcNow);
        return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
    }
}
