using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Services.UserSearch;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup))]
[CheckCanAccessStep(CurrentStep)]
public class DateOfBirthPage : PageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.DateOfBirth;

    private readonly SignInJourney _journey;
    private readonly IUserSearchService _userSearchService;

    public DateOfBirthPage(
        SignInJourney journey,
        IUserSearchService userSearchService)
    {
        _userSearchService = userSearchService;
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    [BindProperty]
    [Display(Name = "Your date of birth", Description = "For example, 27 3 1987")]
    [Required(ErrorMessage = "Enter your date of birth")]
    [IsPastDate(typeof(DateOnly), ErrorMessage = "Your date of birth must be in the past")]
    public DateOnly? DateOfBirth { get; set; }

    public void OnGet()
    {
        SetDefaultInputValues();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var authenticationState = HttpContext.GetAuthenticationState();
        authenticationState.OnDateOfBirthSet((DateOnly)DateOfBirth!);

        var users = await _userSearchService.FindUsers(
            authenticationState.FirstName!,
            authenticationState.LastName!,
            (DateOnly)DateOfBirth!);

        if (users.Length > 0)
        {
            authenticationState.OnExistingAccountFound(users[0]);
        }

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        DateOfBirth ??= _journey.AuthenticationState.DateOfBirth;
    }
}
