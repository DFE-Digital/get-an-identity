using Microsoft.AspNetCore.Mvc;

namespace TeacherIdentityServer.Controllers;

public class AccountController : Controller
{
    [HttpGet("account")]
    public IActionResult Index() => Redirect(Url.Email());
}
