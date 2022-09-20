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
    public async Task<IActionResult> SignOut(string returnUrl)
    {
        await HttpContext.SignOutAsync(scheme: "Cookies");

        var properties = new AuthenticationProperties()
        {
            RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/"
        };

        return SignOut(properties, "oidc");
    }
}
