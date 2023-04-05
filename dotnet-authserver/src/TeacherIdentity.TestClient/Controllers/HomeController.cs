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
            Email = User.FindFirst("email")?.Value,
            UserId = User.FindFirst("sub")?.Value,
            FirstName = User.FindFirst("given_name")?.Value,
            LastName = User.FindFirst("family_name")?.Value,
            Trn = User.FindFirst("trn")?.Value
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
