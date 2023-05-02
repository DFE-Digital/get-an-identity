using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckCanAccessStep(CurrentStep)]
public class HasQtsPage : PageModel
{
    private const string CurrentStep = CoreSignInJourneyWithTrnLookup.Steps.HasQts;

    private readonly SignInJourney _journey;

    public HasQtsPage(SignInJourney journey)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    [BindProperty]
    [Display(Name = "Have you been awarded qualified teacher status (QTS)?")]
    [Required(ErrorMessage = "Tell us if you have been awarded qualified teacher status (QTS)")]
    public bool? AwardedQts { get; set; }

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

        HttpContext.GetAuthenticationState().OnAwardedQtsSet((bool)AwardedQts!);

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        AwardedQts ??= _journey.AuthenticationState.AwardedQts;
    }
}
