using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ResourceServer.Controllers;

public class Api : Controller
{
    [Authorize]
    [Produces("application/json")]
    [HttpGet("resource")]
    public IActionResult Index()
    {
        return Json(new
        {
            Subject = User.FindFirst("sub")?.Value
        });
    }
}
