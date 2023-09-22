using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup))]
[CheckCanAccessStep(CurrentStep)]
public class NoAccount : PageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.NoAccount;

    private readonly SignInJourney _journey;
    private readonly ICurrentClientProvider _currentClientProvider;

    public NoAccount(SignInJourney journey, ICurrentClientProvider currentClientProvider)
    {
        _journey = journey;
        _currentClientProvider = currentClientProvider;
    }

    public string? BackLink => _journey.TryGetPreviousStepUrl(CurrentStep, out var backLink) ? backLink : null;

    public string? EmailAddress => _journey.AuthenticationState.EmailAddress;

    public string? ClientDisplayName { get; set; }

    public TrnMatchPolicy? TrnMatchPolicy { get; set; }

    public async Task OnGet()
    {
        ClientDisplayName = (await _currentClientProvider.GetCurrentClient())?.DisplayName;

        TrnMatchPolicy = _journey.AuthenticationState.OAuthState?.TrnMatchPolicy;
    }

    public async Task<IActionResult> OnPost()
    {
        return await _journey.Advance(CurrentStep);
    }
}
