using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialName;

[OfficialNameChangeEnabled]
public class OfficialName : PageModel
{
    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();
}
