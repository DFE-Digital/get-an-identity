using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.UserSearch;

namespace TeacherIdentity.AuthServer.Pages.SignIn.TrnToken;

[CheckCanAccessStep(CurrentStep)]
public class Landing : PageModel
{
    private const string CurrentStep = TrnTokenSignInJourney.Steps.Landing;

    private readonly SignInJourney _journey;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly TeacherIdentityApplicationManager _applicationManager;
    private readonly IUserSearchService _userSearchService;

    public Landing(
        SignInJourney journey,
        TeacherIdentityServerDbContext dbContext,
        TeacherIdentityApplicationManager applicationManager,
        IUserSearchService userSearchService)
    {
        _journey = journey;
        _dbContext = dbContext;
        _applicationManager = applicationManager;
        _userSearchService = userSearchService;
    }

    public string? ClientDisplayName;

    public async Task<IActionResult> OnPost()
    {
        var authenticationState = _journey.AuthenticationState;

        // Ensure we've got a verified email populated;
        // it may have been unset if the user clicked Sign In, entered an email, then came back here without verifying
        if (!authenticationState.EmailAddressVerified)
        {
            var trnToken = await _dbContext.TrnTokens.SingleAsync(t => t.TrnToken == authenticationState.TrnToken);
            authenticationState.OnEmailSet(trnToken.Email);
            authenticationState.OnEmailVerified(user: null);
        }

        var users = await _userSearchService.FindUsers(
            authenticationState.FirstName!,
            authenticationState.LastName!,
            authenticationState.DateOfBirth!.Value);

        authenticationState.OnExistingAccountSearch(users.Length == 0 ? null : users[0]);

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
