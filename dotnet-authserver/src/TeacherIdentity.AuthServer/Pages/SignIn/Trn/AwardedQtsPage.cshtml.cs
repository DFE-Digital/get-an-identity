using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
[CheckCanAccessStep(CurrentStep)]
public class AwardedQtsPage : PageModel
{
    private const string CurrentStep = LegacyTrnJourney.Steps.AwardedQts;

    private readonly LegacyTrnJourney _journey;

    public AwardedQtsPage(LegacyTrnJourney journey)
    {
        _journey = journey;
    }

    [BindNever]
    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep)!;

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

        _journey.AuthenticationState.OnAwardedQtsSet((bool)AwardedQts!);

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        AwardedQts ??= _journey.AuthenticationState.AwardedQts;
    }
}
