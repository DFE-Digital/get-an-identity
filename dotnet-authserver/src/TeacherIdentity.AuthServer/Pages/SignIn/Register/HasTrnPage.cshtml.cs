using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourneyWithTrnLookup))]
[CheckCanAccessStep(CurrentStep)]
public class HasTrnPage : PageModel
{
    private const string CurrentStep = CoreSignInJourneyWithTrnLookup.Steps.HasTrn;

    private readonly SignInJourney _journey;

    public HasTrnPage(SignInJourney journey)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    [BindProperty]
    [Display(Name = "Do you have a TRN?")]
    [Required(ErrorMessage = "Select yes if you know your TRN")]
    public bool? HasTrn { get; set; }

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

        HttpContext.GetAuthenticationState().OnHasTrnSet(HasTrn!.Value);

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        HasTrn ??= _journey.AuthenticationState.HasTrn;
    }
}
