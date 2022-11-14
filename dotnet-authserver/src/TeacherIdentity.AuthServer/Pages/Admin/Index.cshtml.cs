using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.Admin;

public class IndexModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Admin/Clients");
}
