using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[AllowCompletedAuthenticationJourney]
[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.Complete)]
public class EmailExists : PageModel
{
    public string? Email => HttpContext.GetAuthenticationState().EmailAddress;
}
