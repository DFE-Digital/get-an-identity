using Dfe.Analytics.AspNetCore;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[AllowCompletedAuthenticationJourney]
[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.Complete)]
public class CompleteModel : PageModel
{
    private readonly TeacherIdentityApplicationManager _applicationManager;

    public CompleteModel(TeacherIdentityApplicationManager applicationManager)
    {
        _applicationManager = applicationManager;
    }

    public string? Email { get; set; }

    public bool GotTrn { get; set; }

    public bool FirstTimeSignInForEmail { get; set; }

    public string? Name { get; set; }

    public string? Trn { get; set; }

    public string? RedirectUri { get; set; }

    public string? ResponseMode { get; set; }

    public IEnumerable<KeyValuePair<string, string>>? ResponseParameters { get; set; }

    public bool AlreadyCompleted { get; set; }

    public UserType UserType { get; set; }

    public string? Scope { get; set; }

    public AuthenticationJourneyType? JourneyType { get; set; }

    public string? ClientDisplayName { get; set; }

    public async Task OnGet()
    {
        var authenticationState = HttpContext.GetAuthenticationState();
        authenticationState.EnsureOAuthState();

        RedirectUri = authenticationState.OAuthState.RedirectUri;
        ResponseMode = authenticationState.OAuthState.AuthorizationResponseMode!;
        ResponseParameters = authenticationState.OAuthState.AuthorizationResponseParameters!;
        Email = authenticationState.EmailAddress;
        GotTrn = authenticationState.Trn is not null;
        FirstTimeSignInForEmail = authenticationState.FirstTimeSignInForEmail!.Value;
        Name = $"{authenticationState.FirstName} {authenticationState.LastName}";
        Trn = authenticationState.Trn;
        AlreadyCompleted = authenticationState.HaveResumedCompletedJourney;
        UserType = authenticationState.UserType!.Value;
        Scope = authenticationState.OAuthState?.Scope;
        JourneyType = authenticationState.GetJourneyType();

        var clientId = authenticationState.OAuthState?.ClientId;
        var client = await _applicationManager.FindByClientIdAsync(clientId!);
        ClientDisplayName = await _applicationManager.GetDisplayNameAsync(client!);

        HttpContext.Features.Get<WebRequestEventFeature>()?.Event.AddTag(FirstTimeSignInForEmail ? "FirstTimeUser" : "ReturningUser");
    }
}
