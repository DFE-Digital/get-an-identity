using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Controllers;

public class UserInfoController : Controller
{
    private readonly UserClaimHelper _userClaimHelper;

    public UserInfoController(UserClaimHelper userClaimHelper)
    {
        _userClaimHelper = userClaimHelper;
    }

    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo"), HttpPost("~/connect/userinfo"), Produces("application/json")]
    public async Task<IActionResult> UserInfo()
    {
        var userId = User.GetUserId()!.Value;
        var claims = await _userClaimHelper.GetPublicClaims(userId, User.HasScope);

        if (claims.Count == 0)
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

        var response = claims.ToDictionary(c => c.Type, c => (object)c.Value, StringComparer.Ordinal);
        return Ok(response);
    }
}
