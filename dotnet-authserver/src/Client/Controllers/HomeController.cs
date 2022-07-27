using Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Client.Controllers;

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
            LastName = User.FindFirst("family_name")?.Value
        };

        return View(model);
    }
}
