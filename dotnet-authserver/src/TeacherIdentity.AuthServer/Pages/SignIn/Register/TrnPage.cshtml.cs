using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourneyWithTrnLookup), typeof(ElevateTrnVerificationLevelJourney))]
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
    [Display(Name = "Teacher reference number (TRN)")]
    [Required(ErrorMessage = "Enter your TRN")]
    [RegularExpression(@"\A\D*(\d{1}\D*){7}\D*\Z", ErrorMessage = "Your TRN number should contain 7 digits")]
    public string? StatedTrn { get; set; }

    public bool ShowContinueWithoutTrnButton { get; set; }

    public void OnGet()
    {
        SetDefaultInputValues();
    }

    public async Task<IActionResult> OnPost(string submit)
    {
        if (submit == "trn_not_known" && ShowContinueWithoutTrnButton)
        {
            _journey.AuthenticationState.OnHasTrnSet(false);
        }
        else
        {
            if (!ModelState.IsValid)
            {
                return this.PageWithErrors();
            }

            _journey.AuthenticationState.OnTrnSet(StatedTrn);
        }

        return await _journey.Advance(CurrentStep);
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        ShowContinueWithoutTrnButton = _journey.GetType() != typeof(ElevateTrnVerificationLevelJourney);
    }

    private void SetDefaultInputValues()
    {
        StatedTrn ??= _journey.AuthenticationState.StatedTrn;
    }
}
