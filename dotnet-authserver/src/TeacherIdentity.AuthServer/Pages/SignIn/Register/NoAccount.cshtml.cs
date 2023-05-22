using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup))]
[CheckCanAccessStep(CurrentStep)]
public class NoAccount : PageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.NoAccount;

    private readonly SignInJourney _journey;
    private readonly TeacherIdentityApplicationManager _applicationManager;

    public NoAccount(SignInJourney journey, TeacherIdentityApplicationManager applicationManager)
    {
        _journey = journey;
        _applicationManager = applicationManager;
    }

    public string? BackLink => _journey.TryGetPreviousStepUrl(CurrentStep, out var backLink) ? backLink : null;

    public string? EmailAddress => _journey.AuthenticationState.EmailAddress;

    public string? ClientDisplayName;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        return await _journey.Advance(CurrentStep);
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var clientId = _journey.AuthenticationState.OAuthState?.ClientId;
        var client = await _applicationManager.FindByClientIdAsync(clientId!);
        ClientDisplayName = await _applicationManager.GetDisplayNameAsync(client!);

        await next();
    }
}
