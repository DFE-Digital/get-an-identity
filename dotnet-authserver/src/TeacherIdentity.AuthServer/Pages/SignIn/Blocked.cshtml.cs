using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[CheckCanAccessStep(CoreSignInJourney.Steps.Blocked)]
[AllowCompletedAuthenticationJourney]
public class BlockedModel : PageModel
{
    private readonly ICurrentClientProvider _currentClientProvider;

    public BlockedModel(ICurrentClientProvider currentClientProvider)
    {
        _currentClientProvider = currentClientProvider;
    }

    public string? ClientName { get; set; }

    public async Task OnGet()
    {
        var client = await _currentClientProvider.GetCurrentClient();
        ClientName = client!.DisplayName;
    }
}
