using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

    public static async Task SignInUser(this HttpContext httpContext, User user, string? trn)
    {
        var authenticationState = httpContext.GetAuthenticationState();
        authenticationState.Populate(user, trn);

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

        if (authorizationRequest.HasScope(CustomScopes.Trn) && trn is not null)
        {
            claims.Add(new Claim(CustomClaims.Trn, trn));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "email", nameType: Claims.Name, roleType: null);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
