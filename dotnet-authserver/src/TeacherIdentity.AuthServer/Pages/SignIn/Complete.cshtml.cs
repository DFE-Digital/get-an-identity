using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[AllowCompletedAuthenticationJourney]
public class CompleteModel : PageModel
{
    private readonly IDqtApiClient _dqtApiClient;

    public CompleteModel(IDqtApiClient dqtApiClient)
    {
        _dqtApiClient = dqtApiClient;
    }

    public string? Email { get; set; }

    public bool GotTrn { get; set; }

    public bool FirstTimeSignInForEmail { get; set; }

    public string? Name { get; set; }

    public string? DqtName { get; set; }

    public string? Trn { get; set; }

    public string? RedirectUri { get; set; }

    public string? ResponseMode { get; set; }

    public IEnumerable<KeyValuePair<string, string>>? ResponseParameters { get; set; }

    public bool AlreadyCompleted { get; set; }

    public UserType UserType { get; set; }

    public async Task OnGet()
    {
        var authenticationState = HttpContext.GetAuthenticationState();
        authenticationState.EnsureOAuthState();

        if (authenticationState.Trn is not null)
        {
            var teacherInfo = await _dqtApiClient.GetTeacherByTrn(authenticationState.Trn);

            if (teacherInfo is null)
            {
                throw new Exception($"DQT API lookup failed for TRN {authenticationState.Trn}.");
            }

            DqtName = $"{teacherInfo.FirstName} {teacherInfo.LastName}";
        }

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
    }
}
