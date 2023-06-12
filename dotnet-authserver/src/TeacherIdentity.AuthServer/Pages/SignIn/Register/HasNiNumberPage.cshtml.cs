using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourneyWithTrnLookup))]
[CheckCanAccessStep(CurrentStep)]
public class HasNiNumberPage : PageModel
{
    private const string CurrentStep = CoreSignInJourneyWithTrnLookup.Steps.HasNiNumber;

    private readonly SignInJourney _journey;

    public HasNiNumberPage(SignInJourney journey)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    [BindProperty]
    [Display(Name = "Do you have a National Insurance number?")]
    [Required(ErrorMessage = "Select yes if you have a National Insurance number")]
    public bool? HasNiNumber { get; set; }

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

        HttpContext.GetAuthenticationState().OnHasNationalInsuranceNumberSet((bool)HasNiNumber!);

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        HasNiNumber ??= HttpContext.GetAuthenticationState().HasNationalInsuranceNumber;
    }
}
