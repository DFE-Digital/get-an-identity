using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace TeacherIdentityServer.Controllers;

public class TempController : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (authenticateResult == null || !authenticateResult.Succeeded)
        {
            return Challenge(authenticationSchemes: CookieAuthenticationDefaults.AuthenticationScheme);
        }

        return Content($"Signed in as {authenticateResult!.Principal!.Identity!.Name}");
    }
}
