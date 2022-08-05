using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.State;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer;

public static class HttpContextExtensions
{
    public static AuthenticationState GetAuthenticationState(this HttpContext httpContext) =>
        httpContext.Features.Get<AuthenticationStateFeature>()?.AuthenticationState ??
            throw new InvalidOperationException($"The current request has no {nameof(AuthenticationState)}.");

    public static async Task<IActionResult> SignInUser(this HttpContext httpContext, TeacherIdentityUser user)
    {
        var authenticationState = httpContext.GetAuthenticationState();
        var authorizationRequest = authenticationState.GetAuthorizationRequest();

        var claims = new List<Claim>()
        {
            new Claim(Claims.Subject, user.UserId.ToString()),
            new Claim(Claims.Email, user.EmailAddress!),
            new Claim(Claims.EmailVerified, "true"),
            new Claim(Claims.Name, user.FirstName + " " + user.LastName),
            new Claim(Claims.GivenName, user.FirstName!),
            new Claim(Claims.FamilyName, user.LastName!)
        };

        if (authorizationRequest.HasScope(CustomScopes.Trn) && user.Trn is not null)
        {
            claims.Add(new Claim(CustomClaims.Trn, user.Trn));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "email", nameType: Claims.Name, roleType: null);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return new RedirectResult(authenticationState.AuthorizationUrl);
    }
}
