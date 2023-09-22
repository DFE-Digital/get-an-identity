using Dfe.Analytics.AspNetCore;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[AllowCompletedAuthenticationJourney]
public class CompleteModel : PageModel
{
    private readonly SignInJourney _journey;
    private readonly TeacherIdentityApplicationManager _applicationManager;
    private readonly TeacherIdentityServerDbContext _dbContext;

    public CompleteModel(
        SignInJourney journey,
        TeacherIdentityApplicationManager applicationManager,
        TeacherIdentityServerDbContext dbContext)
    {
        _journey = journey;
        _applicationManager = applicationManager;
        _dbContext = dbContext;
    }

    public string? Email { get; set; }

    public bool FirstTimeSignInForEmail { get; set; }

    public string? Trn { get; set; }

    public string? RedirectUri { get; set; }

    public string? ResponseMode { get; set; }

    public IEnumerable<KeyValuePair<string, string>>? ResponseParameters { get; set; }

    public TrnLookupStatus? TrnLookupStatus { get; set; }

    public TrnRequirementType? TrnRequirementType { get; set; }

    public string? ClientDisplayName { get; set; }

    public bool TrnLookupSupportTicketCreated { get; set; }

    public bool CanAccessService { get; set; }

    public async Task OnGet()
    {
        var authenticationState = HttpContext.GetAuthenticationState();
        authenticationState.EnsureOAuthState();

        RedirectUri = authenticationState.OAuthState.RedirectUri;
        ResponseMode = authenticationState.OAuthState.AuthorizationResponseMode!;
        ResponseParameters = authenticationState.OAuthState.AuthorizationResponseParameters!;
        CanAccessService = authenticationState.OAuthState?.TrnRequirementType != Models.TrnRequirementType.Required || authenticationState.Trn is not null;
        Email = authenticationState.EmailAddress;
        FirstTimeSignInForEmail = authenticationState.FirstTimeSignInForEmail!.Value;
        Trn = authenticationState.Trn;
        TrnLookupStatus = authenticationState.TrnLookupStatus;
        TrnRequirementType = authenticationState.OAuthState?.TrnRequirementType;

        var user = await _dbContext.Users.SingleAsync(u => u.UserId == authenticationState.UserId);
        TrnLookupSupportTicketCreated = user?.TrnLookupSupportTicketCreated == true;

        var clientId = authenticationState.OAuthState?.ClientId;
        var client = await _applicationManager.FindByClientIdAsync(clientId!);
        ClientDisplayName = await _applicationManager.GetDisplayNameAsync(client!);

        HttpContext.Features.Get<WebRequestEventFeature>()?.Event.AddTag(FirstTimeSignInForEmail ? "FirstTimeUser" : "ReturningUser");
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (!_journey.IsCompleted())
        {
            context.Result = Redirect(_journey.GetLastAccessibleStepUrl(requestedStep: null));
        }
    }
}
