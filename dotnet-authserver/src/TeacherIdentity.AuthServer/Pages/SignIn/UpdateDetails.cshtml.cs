using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[AllowCompletedAuthenticationJourney]
public class UpdateDetailsModel : PageModel
{
    private readonly IDqtApiClient _dqtApiClient;

    public UpdateDetailsModel(IDqtApiClient dqtApiClient)
    {
        _dqtApiClient = dqtApiClient;
    }

    public string? DqtName { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public UserType UserType { get; set; }

    public async Task OnGet()
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        if (authenticationState.Trn is not null)
        {
            var teacherInfo = await _dqtApiClient.GetTeacherByTrn(authenticationState.Trn);

            if (teacherInfo is null)
            {
                throw new Exception($"DQT API lookup failed for TRN {authenticationState.Trn}.");
            }

            DqtName = $"{teacherInfo.FirstName} {teacherInfo.LastName}";
        }

        Name = $"{authenticationState.FirstName} {authenticationState.LastName}";
        Email = authenticationState.EmailAddress;
        UserType = authenticationState.UserType!.Value;
    }
}
