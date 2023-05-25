using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.TrnToken;

[CheckJourneyType(typeof(TrnTokenSignInJourney))]
[CheckCanAccessStep(CurrentStep)]
public class CheckAnswers : PageModel
{
    private const string CurrentStep = TrnTokenSignInJourney.Steps.CheckAnswers;

    private readonly SignInJourney _journey;

    public CheckAnswers(SignInJourney journey)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);
    public string? EmailAddress => _journey.AuthenticationState.EmailAddress;
    public string? MobilePhoneNumber => _journey.AuthenticationState.MobileNumber;
    public string? FirstName => _journey.AuthenticationState.FirstName;
    public string? MiddleName => _journey.AuthenticationState.MiddleName;
    public string? LastName => _journey.AuthenticationState.LastName;
    public DateOnly? DateOfBirth => _journey.AuthenticationState.DateOfBirth;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        return await _journey.CreateUser(CurrentStep);
    }
}