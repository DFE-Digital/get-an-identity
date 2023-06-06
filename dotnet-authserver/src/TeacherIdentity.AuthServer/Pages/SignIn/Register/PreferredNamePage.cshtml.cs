using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup), typeof(TrnTokenSignInJourney))]
[CheckCanAccessStep(CurrentStep)]
public class PreferredNamePage : PageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.PreferredName;

    private SignInJourney _journey;

    public PreferredNamePage(SignInJourney journey)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    public string ExistingName => _journey.AuthenticationState.GetName()!;

    [BindProperty]
    [Display(Name = "Use preferred name?")]
    [Required(ErrorMessage = "Select which name to use")]
    public bool? HasPreferredName { get; set; }

    [BindProperty]
    [Display(Name = "Your preferred name")]
    [RequiredIfTrue(nameof(HasPreferredName), ErrorMessage = "Enter your preferred name")]
    [StringLengthIfTrue(nameof(HasPreferredName), 200, ErrorMessage = "Preferred name must be 200 characters or less")]
    public string? PreferredName { get; set; }

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

        HttpContext.GetAuthenticationState().OnPreferredNameSet(PreferredName ?? ExistingName);

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        var authState = _journey.AuthenticationState;

        if (!string.IsNullOrEmpty(authState.PreferredName) && authState.PreferredName != authState.GetName())
        {
            HasPreferredName = true;
            PreferredName = authState.PreferredName;
        }
    }
}
