using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
[CheckCanAccessStep(CurrentStep)]
public class HasNiNumberPage : PageModel
{
    private const string CurrentStep = LegacyTrnJourney.Steps.HasNationalInsuranceNumber;

    private readonly LegacyTrnJourney _journey;

    public HasNiNumberPage(LegacyTrnJourney journey)
    {
        _journey = journey;
    }

    [BindNever]
    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep)!;

    [Display(Name = "Do you have a National Insurance number?")]
    [Required(ErrorMessage = "Tell us if you have a National Insurance number")]
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

        _journey.AuthenticationState.OnHasNationalInsuranceNumberSet((bool)HasNiNumber!);

        return (bool)HasNiNumber! ?
            Redirect(_journey.GetNextStepUrl(CurrentStep)) :
            await _journey.FindTrnAndContinue(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        HasNiNumber ??= HttpContext.GetAuthenticationState().HasNationalInsuranceNumber;
    }
}
