using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
[CheckCanAccessStep(CurrentStep)]
public class HasTrnPage : PageModel
{
    private const string CurrentStep = LegacyTrnJourney.Steps.HasTrn;

    private readonly LegacyTrnJourney _journey;

    public HasTrnPage(LegacyTrnJourney journey)
    {
        _journey = journey;
    }

    // Properties are set in the order that they are declared. Because the value of HasTrn
    // is used in the conditional RequiredIfTrue attribute, it should be set first.
    [Display(Name = "Do you know your TRN?")]
    [Required(ErrorMessage = "Tell us if you know your TRN")]
    public bool? HasTrn { get; set; }

    [Display(Name = "What is your TRN?")]
    [RequiredIfTrue(nameof(HasTrn), ErrorMessage = "Enter your TRN")]
    [RegexIfTrue(nameof(HasTrn), @"\A\D*(\d{1}\D*){7}\D*\Z", ErrorMessage = "Your TRN number should contain 7 digits")]
    public string? StatedTrn { get; set; }

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

        _journey.AuthenticationState.OnTrnSet(StatedTrn);

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        HasTrn ??= _journey.AuthenticationState.HasTrn;
        StatedTrn ??= _journey.AuthenticationState.StatedTrn;
    }
}
