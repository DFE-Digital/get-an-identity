using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Elevate;

[CheckCanAccessStep(CurrentStep)]
public class Landing : PageModel
{
    private const string CurrentStep = ElevateTrnVerificationLevelJourney.Steps.Landing;

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

    public TrnRequirementType TrnRequirementType { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost() => await _journey.Advance(CurrentStep);

    public async override Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        ClientDisplayName = (await _currentClientProvider.GetCurrentClient())!.DisplayName;
        TrnRequirementType = _journey.AuthenticationState.OAuthState!.TrnRequirementType!.Value;

        await base.OnPageHandlerExecutionAsync(context, next);
    }
}
