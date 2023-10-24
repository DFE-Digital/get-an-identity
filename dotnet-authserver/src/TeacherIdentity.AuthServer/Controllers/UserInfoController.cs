using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Controllers;

public class UserInfoController : Controller
{
    private readonly UserClaimHelper _userClaimHelper;

    public UserInfoController(UserClaimHelper userClaimHelper)
    {
        _userClaimHelper = userClaimHelper;
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Oidc)]
    [HttpGet("~/connect/userinfo"), HttpPost("~/connect/userinfo"), Produces("application/json")]
    public async Task<IActionResult> UserInfo()
    {
        var userId = User.GetUserId();

        TrnMatchPolicy? trnMatchPolicy = User.GetClaim(CustomClaims.Private.TrnMatchPolicy) is string trnMatchPolicyStr ?
            Enum.Parse<TrnMatchPolicy>(trnMatchPolicyStr) :
            null;

        var claims = await _userClaimHelper.GetPublicClaims(userId, trnMatchPolicy);

        if (claims.Count == 0)
        {
            return Challenge(
                authenticationSchemes: AuthenticationSchemes.Oidc,
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
