using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[AllowCompletedAuthenticationJourney]
[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.Complete)]
public class PhoneExists : PageModel
{
    public string? MobileNumber => HttpContext.GetAuthenticationState().MobileNumber;
}
