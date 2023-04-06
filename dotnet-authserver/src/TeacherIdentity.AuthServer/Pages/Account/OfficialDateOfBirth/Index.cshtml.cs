using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialDateOfBirth;

[CheckOfficialDateOfBirthChangeIsEnabled]
public class OfficialDateOfBirth : PageModel
{
    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();
}
