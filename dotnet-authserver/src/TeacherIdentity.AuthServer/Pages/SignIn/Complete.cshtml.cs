using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class CompleteModel : PageModel
{
    public string? Email { get; set; }

    public bool GotTrn { get; set; }

    public bool FirstTimeUser { get; set; }

    public string? Name { get; set; }

    public string? Trn { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public string? RedirectUri { get; set; }

    public string? ResponseMode { get; set; }

    public IEnumerable<KeyValuePair<string, string>>? ResponseParameters { get; set; }

    public bool AlreadyCompleted { get; set; }

    public bool ShowTrnRow { get; set; }

    public void OnGet()
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        RedirectUri = authenticationState.RedirectUri;
        ResponseMode = authenticationState.AuthorizationResponseMode!;
        ResponseParameters = authenticationState.AuthorizationResponseParameters!;
        Email = authenticationState.EmailAddress;
        GotTrn = authenticationState.Trn is not null;
        FirstTimeUser = authenticationState.FirstTimeUser!.Value;
        Name = $"{authenticationState.FirstName} {authenticationState.LastName}";
        Trn = authenticationState.Trn;
        DateOfBirth = authenticationState.DateOfBirth!.Value;
        AlreadyCompleted = authenticationState.HaveResumedCompletedJourney;
        ShowTrnRow = authenticationState.GetUserType() == Models.UserType.Teacher;
    }
}
