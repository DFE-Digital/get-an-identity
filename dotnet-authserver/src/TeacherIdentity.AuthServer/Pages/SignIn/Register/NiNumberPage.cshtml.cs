using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourneyWithTrnLookup), typeof(ElevateTrnVerificationLevelJourney))]
[CheckCanAccessStep(CurrentStep)]
public class NiNumberPage : PageModel
{
    private const string CurrentStep = CoreSignInJourneyWithTrnLookup.Steps.NiNumber;

    private readonly SignInJourney _journey;

    public NiNumberPage(SignInJourney journey)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    [BindProperty]
    [Display(Name = "What is your National Insurance number?", Description = "It’s on your National Insurance card, benefit letter, payslip or P60. For example, ‘QQ 12 34 56 C’.")]
    [Required(ErrorMessage = "Enter a National Insurance number")]
    [NationalInsuranceNumber(ErrorMessage = "Enter a National Insurance number in the correct format")]
    public string? NiNumber { get; set; }

    public void OnGet()
    {
        SetDefaultInputValues();
    }

    public async Task<IActionResult> OnPost(string submit)
    {
        if (submit == "ni_number_not_known")
        {
            _journey.AuthenticationState.OnHasNationalInsuranceNumberSet(false);
        }
        else
        {
            if (!ModelState.IsValid)
            {
                return this.PageWithErrors();
            }

            _journey.AuthenticationState.OnNationalInsuranceNumberSet(NiNumber!);
        }

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        NiNumber ??= _journey.AuthenticationState.NationalInsuranceNumber;
    }
}
