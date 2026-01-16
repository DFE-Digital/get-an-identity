using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
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
        ICurrentClientProvider currentClientProvider,
        IOptions<PreventRegistrationOptions> preventRegistrationOptions)
    {
        _journey = journey;
        _currentClientProvider = currentClientProvider;
        PreventRegistrationOptions = preventRegistrationOptions.Value;
    }

    public string? ClientDisplayName { get; set; }
    public TrnMatchPolicy? TrnMatchPolicy { get; set; }
    public PreventRegistrationOptions PreventRegistrationOptions { get; }
    public Application? CurrentClient { get; private set; }

    public async Task OnGet()
    {
        CurrentClient = await _currentClientProvider.GetCurrentClient();
        ClientDisplayName = CurrentClient?.DisplayName;
        TrnMatchPolicy = _journey.AuthenticationState.OAuthState?.TrnMatchPolicy;
    }
}
