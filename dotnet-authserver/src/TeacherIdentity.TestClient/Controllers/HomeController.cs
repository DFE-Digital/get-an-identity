using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.TestClient.Models;

namespace TeacherIdentity.TestClient.Controllers;

public class HomeController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();

    [Authorize]
    [HttpGet("profile")]
    public IActionResult Profile()
    {
        var model = new ProfileModel()
        {
            Email = User.FindFirstValue("email"),
            UserId = User.FindFirstValue("sub"),
            FirstName = User.FindFirstValue("given_name"),
            LastName = User.FindFirstValue("family_name"),
            PreferredName = User.FindFirstValue("preferred-name"),
            Trn = User.FindFirstValue("trn")
        };

        return View(model);
    }

    [HttpGet("sign-out")]
    public new IActionResult SignOut() => View();

    [HttpPost("sign-out")]
    public async Task<IActionResult> SignOutPost()
    {
        await HttpContext.SignOutAsync(scheme: "Cookies");

        var properties = new AuthenticationProperties()
        {
            RedirectUri = "/"
        };

        return SignOut(properties, "oidc");
    }
}
