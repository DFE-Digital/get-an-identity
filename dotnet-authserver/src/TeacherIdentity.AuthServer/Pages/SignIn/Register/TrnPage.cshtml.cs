using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourneyWithTrnLookup))]
[CheckCanAccessStep(CurrentStep)]
public class TrnPage : PageModel
{
    private const string CurrentStep = CoreSignInJourneyWithTrnLookup.Steps.Trn;

    private readonly SignInJourney _journey;

    public TrnPage(SignInJourney journey)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    [BindProperty]
    [Display(Name = "Enter your TRN")]
    [Required(ErrorMessage = "Enter your TRN")]
    [RegularExpression(@"\A\D*(\d{1}\D*){7}\D*\Z", ErrorMessage = "Your TRN number should contain 7 digits")]
    public string? StatedTrn { get; set; }

    public void OnGet()
    {
        SetDefaultInputValues();
    }

    public async Task<IActionResult> OnPost(string submit)
    {
        if (submit == "trn_not_known")
        {
            HttpContext.GetAuthenticationState().OnHasTrnSet(false);
        }
        else
        {
            if (!ModelState.IsValid)
            {
                return this.PageWithErrors();
            }

            HttpContext.GetAuthenticationState().OnTrnSet(StatedTrn);
        }

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        StatedTrn ??= _journey.AuthenticationState.StatedTrn;
    }
}
