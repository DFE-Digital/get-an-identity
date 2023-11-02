using Dfe.Analytics.AspNetCore;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[AllowCompletedAuthenticationJourney]
public class CompleteModel : PageModel
{
    private readonly SignInJourney _journey;
    private readonly TeacherIdentityApplicationManager _applicationManager;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IDqtApiClient _dqtApiClient;

    public CompleteModel(
        SignInJourney journey,
        TeacherIdentityApplicationManager applicationManager,
        TeacherIdentityServerDbContext dbContext,
        IDqtApiClient dqtApiClient)
    {
        _journey = journey;
        _applicationManager = applicationManager;
        _dbContext = dbContext;
        _dqtApiClient = dqtApiClient;
    }

    public string? Email { get; set; }

    public bool FirstTimeSignInForEmail { get; set; }

    public string? Trn { get; set; }

    public bool? TrnVerificationElevationSuccessful { get; set; }

    public string? RedirectUri { get; set; }

    public string? ResponseMode { get; set; }

    public IEnumerable<KeyValuePair<string, string>>? ResponseParameters { get; set; }

    public TrnLookupStatus? TrnLookupStatus { get; set; }

    public TrnRequirementType? TrnRequirementType { get; set; }

    public TrnMatchPolicy? TrnMatchPolicy { get; set; }

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
        Email = authenticationState.EmailAddress;
        FirstTimeSignInForEmail = authenticationState.FirstTimeSignInForEmail!.Value;
        Trn = authenticationState.Trn;
        TrnVerificationElevationSuccessful = authenticationState.TrnVerificationElevationSuccessful;
        TrnLookupStatus = authenticationState.TrnLookupStatus;
        TrnRequirementType = authenticationState.OAuthState.TrnRequirementType;
        TrnMatchPolicy = authenticationState.OAuthState.TrnMatchPolicy;

        var user = await _dbContext.Users.SingleAsync(u => u.UserId == authenticationState.UserId);
        TrnLookupSupportTicketCreated = user?.TrnLookupSupportTicketCreated == true;

        var clientId = authenticationState.OAuthState.ClientId;
        var client = await _applicationManager.FindByClientIdAsync(clientId!);
        ClientDisplayName = await _applicationManager.GetDisplayNameAsync(client!);

        CanAccessService = authenticationState.OAuthState.TrnRequirementType switch
        {
            Models.TrnRequirementType.Required => authenticationState.Trn is not null,
            _ => true
        };

        if (authenticationState.OAuthState.TrnRequirementType == Models.TrnRequirementType.Required &&
            authenticationState.OAuthState.BlockProhibitedTeachers == true &&
            authenticationState.Trn is string trn &&
            CanAccessService)
        {
            var dqtUser = await _dqtApiClient.GetTeacherByTrn(trn) ??
                throw new Exception($"Failed to retreive teacher with TRN {trn} from DQT.");

            if (dqtUser.Alerts.Any(a => a.AlertType == AlertType.Prohibition))
            {
                CanAccessService = false;
            }
        }

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
