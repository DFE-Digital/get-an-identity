using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[IgnoreAntiforgeryToken]
public class TrnCallbackModel : PageModel
{
    private readonly FindALostTrnIntegrationHelper _findALostTrnIntegrationHelper;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly ILogger<TrnCallbackModel> _logger;
    private readonly IDqtApiClient _dqtApiClient;

    public TrnCallbackModel(
        FindALostTrnIntegrationHelper findALostTrnIntegrationHelper,
        TeacherIdentityServerDbContext dbContext,
        ILogger<TrnCallbackModel> logger,
        IConfiguration configuration,
        IDqtApiClient apiClient)
    {
        _findALostTrnIntegrationHelper = findALostTrnIntegrationHelper;
        _dbContext = dbContext;
        _logger = logger;
        _dqtApiClient = apiClient;
    }

    public async Task<IActionResult> OnPost()
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        var requestUrl = UriHelper.GetEncodedUrl(Request);
        var formValues = Request.HasFormContentType ?
            Request.Form.ToDictionary(f => f.Key, f => f.Value.ToString())
            : null;

        if (!_findALostTrnIntegrationHelper.ValidateCallback(formValues, out var findALostTrnUser))
        {
            return BadRequest();
        }

        // We don't expect to have an existing user at this point
        if (authenticationState.UserId.HasValue)
        {
            throw new NotImplementedException();
        }

        if (!RequiredClaimsAreProvided(findALostTrnUser, Claims.GivenName, Claims.FamilyName, Claims.Birthdate))
        {
            return BadRequest();
        }

        var userId = Guid.NewGuid();
        var user = new User()
        {
            DateOfBirth = DateOnly.ParseExact(findALostTrnUser.FindFirst(Claims.Birthdate)!.Value, "yyyy-MM-dd"),
            EmailAddress = authenticationState.EmailAddress!,
            FirstName = findALostTrnUser.FindFirst(Claims.GivenName)!.Value,
            LastName = findALostTrnUser.FindFirst(Claims.FamilyName)!.Value,
            UserId = userId
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var trn = findALostTrnUser.FindFirst(CustomClaims.Trn)!.Value;
        authenticationState.Trn = trn;

        await _dqtApiClient.SetTeacherIdentityInfo(new DqtTeacherIdentityInfo() { Trn = trn!, TsPersonId = userId.ToString() });

        await HttpContext.SignInUser(user, authenticationState.Trn!);

        return Redirect(authenticationState.GetNextHopUrl(Url));

        bool RequiredClaimsAreProvided(ClaimsPrincipal principal, params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                if (!principal.HasClaim(c => c.Type == claimType))
                {
                    _logger.LogError("Required claim '{ClaimType}' was not provided.", claimType);
                    return false;
                }
            }

            return true;
        }
    }
}
