using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Controllers;

public class UserInfoController : Controller
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly UserClaimHelper _userClaimHelper;

    public UserInfoController(TeacherIdentityServerDbContext dbContext, UserClaimHelper userClaimHelper)
    {
        _dbContext = dbContext;
        _userClaimHelper = userClaimHelper;
    }

    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo"), HttpPost("~/connect/userinfo"), Produces("application/json")]
    public async Task<IActionResult> UserInfo()
    {
        var userId = Guid.Parse(User.FindFirst(Claims.Subject)!.Value);
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == userId);

        if (user is null)
        {
            return Challenge(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>()
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The specified access token is bound to an account that no longer exists."
                }));
        }

        var claims = _userClaimHelper.GetPublicClaims(user, User.HasScope);
        var response = claims.ToDictionary(c => c.Type, c => (object)c.Value, StringComparer.Ordinal);
        return Ok(response);
    }
}
