using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Services.UserSearch;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup))]
[CheckCanAccessStep(CurrentStep)]
public class Name : PageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.Name;

    private SignInJourney _journey;
    private readonly IUserSearchService _userSearchService;

    public Name(
        SignInJourney journey,
        IUserSearchService userSearchService)
    {
        _journey = journey;
        _userSearchService = userSearchService;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    [BindProperty]
    [Display(Name = "First name")]
    [Required(ErrorMessage = "Enter your first name")]
    [StringLength(200, ErrorMessage = "First name must be 200 characters or less")]
    public string? FirstName { get; set; }

    [BindProperty]
    [Display(Name = "Middle name (optional)")]
    [StringLength(200, ErrorMessage = "Middle name must be 200 characters or less")]
    public string? MiddleName { get; set; }

    [BindProperty]
    [Display(Name = "Last name")]
    [Required(ErrorMessage = "Enter your last name")]
    [StringLength(200, ErrorMessage = "Last name must be 200 characters or less")]
    public string? LastName { get; set; }

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

        if ((authenticationState.FirstName != FirstName || authenticationState.LastName != LastName) &&
            authenticationState.DateOfBirthSet)
        {
            var users = await _userSearchService.FindUsers(
                FirstName!,
                LastName!,
                authenticationState.DateOfBirth!.Value);

            authenticationState.OnExistingAccountSearch(users.Length == 0 ? null : users[0]);
        }

        HttpContext.GetAuthenticationState().OnNameSet(FirstName!, MiddleName, LastName!);

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        FirstName ??= _journey.AuthenticationState.FirstName;
        MiddleName ??= _journey.AuthenticationState.MiddleName;
        LastName ??= _journey.AuthenticationState.LastName;
    }
}
