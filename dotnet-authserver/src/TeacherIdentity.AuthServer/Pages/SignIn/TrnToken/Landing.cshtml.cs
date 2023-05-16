using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Pages.SignIn.TrnToken;

[CheckCanAccessStep(CurrentStep)]
public class Landing : PageModel
{
    private const string CurrentStep = TrnTokenSignInJourney.Steps.Landing;

    private readonly SignInJourney _journey;
    private readonly TeacherIdentityApplicationManager _applicationManager;

    public Landing(SignInJourney journey, TeacherIdentityApplicationManager applicationManager)
    {
        _journey = journey;
        _applicationManager = applicationManager;
    }

    public string? ClientDisplayName;

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var clientId = _journey.AuthenticationState.OAuthState?.ClientId;
        var client = await _applicationManager.FindByClientIdAsync(clientId!);
        ClientDisplayName = await _applicationManager.GetDisplayNameAsync(client!);

        await next();
    }
}
