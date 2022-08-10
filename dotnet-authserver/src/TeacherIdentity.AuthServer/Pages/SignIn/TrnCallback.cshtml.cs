using System.Security.Claims;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[IgnoreAntiforgeryToken]
public class TrnCallbackModel : PageModel
{
    private readonly FindALostTrnIntegrationHelper _findALostTrnIntegrationHelper;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly ILogger<TrnCallbackModel> _logger;

    public TrnCallbackModel(
        FindALostTrnIntegrationHelper findALostTrnIntegrationHelper,
        TeacherIdentityServerDbContext dbContext,
        ILogger<TrnCallbackModel> logger)
    {
        _findALostTrnIntegrationHelper = findALostTrnIntegrationHelper;
        _dbContext = dbContext;
        _logger = logger;
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

        if (!RequiredClaimsAreProvided(findALostTrnUser, "trn", Claims.GivenName, Claims.FamilyName, Claims.Birthdate))
        {
            return BadRequest();
        }

        authenticationState.Trn = findALostTrnUser.FindFirst("trn")!.Value;
        authenticationState.FirstName = findALostTrnUser.FindFirst(Claims.GivenName)!.Value;
        authenticationState.LastName = findALostTrnUser.FindFirst(Claims.FamilyName)!.Value;
        authenticationState.DateOfBirth = DateOnly.ParseExact(findALostTrnUser.FindFirst(Claims.Birthdate)!.Value, "yyyy-MM-dd");

        var userId = Guid.NewGuid();
        var user = new User()
        {
            DateOfBirth = authenticationState.DateOfBirth!.Value,
            EmailAddress = authenticationState.EmailAddress!,
            FirstName = authenticationState.FirstName,
            LastName = authenticationState.LastName,
            Trn = authenticationState.Trn,
            UserId = userId
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        authenticationState.UserId = userId;

        return await HttpContext.SignInUser(user);

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
