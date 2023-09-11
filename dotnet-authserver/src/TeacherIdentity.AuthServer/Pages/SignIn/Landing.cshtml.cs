using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[CheckCanAccessStep(CurrentStep)]
public class Landing : PageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.Landing;

    private readonly SignInJourney _journey;
    private readonly ICurrentClientProvider _currentClientProvider;

    public Landing(
        SignInJourney journey,
        ICurrentClientProvider currentClientProvider)
    {
        _journey = journey;
        _currentClientProvider = currentClientProvider;
    }

    public string? ClientDisplayName { get; set; }

    public TrnMatchPolicy? TrnMatchPolicy { get; set; }

    public async override Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        ClientDisplayName = (await _currentClientProvider.GetCurrentClient())?.DisplayName;

        TrnMatchPolicy = _journey.AuthenticationState.OAuthState?.TrnMatchPolicy;

        await base.OnPageHandlerExecutionAsync(context, next);
    }
}
