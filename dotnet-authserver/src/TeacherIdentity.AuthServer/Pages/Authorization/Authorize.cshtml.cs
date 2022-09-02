using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace TeacherIdentity.AuthServer.Pages.Authorization;

public class AuthorizeModel : PageModel
{
    public ApplyAuthorizationResponseContext? ApplyAuthorizationResponseContext { get; set; }

    public string? Email { get; set; }

    public bool GotTrn { get; set; }

    [FromQuery(Name = "ftu")]
    public bool FirstTimeUser { get; set; }

    public string? Name { get; set; }

    public string? Trn { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public async Task OnGet()
    {
        ApplyAuthorizationResponseContext = HttpContext.Features.Get<ApplyAuthorizationResponseContext>();
        if (ApplyAuthorizationResponseContext is null)
        {
            throw new InvalidOperationException($"No {nameof(ApplyAuthorizationResponseContext)} set.");
        }

        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (authenticateResult is null || !authenticateResult.Succeeded)
        {
            throw new InvalidOperationException("User is not signed in.");
        }

        var user = authenticateResult.Principal;

        Email = user.FindFirst(Claims.Email)!.Value;
        Trn = user.FindFirst(CustomClaims.Trn)?.Value;
        GotTrn = Trn is not null;
        Name = $"{user.FindFirst(Claims.GivenName)!.Value} {user.FindFirst(Claims.FamilyName)!.Value}";
        DateOfBirth = DateOnly.ParseExact(user.FindFirst(Claims.Birthdate)!.Value, "yyyy-MM-dd");
    }
}
