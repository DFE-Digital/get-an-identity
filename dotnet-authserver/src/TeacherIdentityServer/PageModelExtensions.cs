using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentityServer.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentityServer;

public static class PageModelExtensions
{
    public static async Task<IActionResult> SignInUser(this PageModel page, TeacherIdentityUser user)
    {
        var authenticateState = page.HttpContext.GetAuthenticationState();

        var claims = new[]
        {
            new Claim(Claims.Subject, user.UserId.ToString()),
            new Claim(Claims.Email, user.EmailAddress!),
            new Claim(Claims.EmailVerified, "true"),
            new Claim(Claims.Name, user.FirstName + " " + user.LastName),
            new Claim(Claims.GivenName, user.FirstName!),
            new Claim(Claims.FamilyName, user.LastName!),
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "email", nameType: Claims.Name, roleType: null);
        var principal = new ClaimsPrincipal(identity);

        await page.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return page.LocalRedirect(authenticateState.AuthorizationUrl);
    }
}
