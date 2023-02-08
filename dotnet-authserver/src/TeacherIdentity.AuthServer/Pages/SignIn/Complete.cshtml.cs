using Dfe.Analytics.AspNetCore;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[AllowCompletedAuthenticationJourney]
[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.Complete)]
public class CompleteModel : PageModel
{
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

    public void OnGet()
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

        HttpContext.Features.Get<WebRequestEventFeature>()?.Event.AddTag(FirstTimeSignInForEmail ? "FirstTimeUser" : "ReturningUser");
    }
}
