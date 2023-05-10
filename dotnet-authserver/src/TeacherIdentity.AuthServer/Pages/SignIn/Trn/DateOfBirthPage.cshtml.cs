using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
[CheckCanAccessStep(CurrentStep)]
public class DateOfBirthPage : PageModel
{
    private const string CurrentStep = LegacyTrnJourney.Steps.DateOfBirth;

    private readonly LegacyTrnJourney _journey;

    public DateOfBirthPage(LegacyTrnJourney journey)
    {
        _journey = journey;
    }

    [BindNever]
    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep)!;

    [Display(Name = "Your date of birth", Description = "For example, 27 3 1987")]
    [Required(ErrorMessage = "Enter your date of birth")]
    [IsValidDateOfBirth(typeof(DateOnly))]
    public DateOnly? DateOfBirth { get; set; }

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

        _journey.AuthenticationState.OnDateOfBirthSet((DateOnly)DateOfBirth!);

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        DateOfBirth ??= _journey.AuthenticationState.DateOfBirth;
    }
}
