using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using static TeacherIdentity.AuthServer.AuthenticationState;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
[CheckCanAccessStep(CurrentStep)]
public class OfficialName : PageModel
{
    private const string CurrentStep = LegacyTrnJourney.Steps.OfficialName;

    private readonly LegacyTrnJourney _journey;

    public OfficialName(LegacyTrnJourney journey)
    {
        _journey = journey;
    }

    [BindNever]
    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep)!;

    [Display(Name = "First name")]
    [Required(ErrorMessage = "Enter your first name")]
    [StringLength(200, ErrorMessage = "First name must be 200 characters or less")]
    public string? OfficialFirstName { get; set; }

    [Display(Name = "Last name")]
    [Required(ErrorMessage = "Enter your last name")]
    [StringLength(200, ErrorMessage = "Last name must be 200 characters or less")]
    public string? OfficialLastName { get; set; }

    [Display(Name = "Previous first name (optional)")]
    public string? PreviousOfficialFirstName { get; set; }

    [Display(Name = "Previous last name (optional)")]
    public string? PreviousOfficialLastName { get; set; }

    [Required(ErrorMessage = "Tell us if you have changed your name")]
    public HasPreviousNameOption? HasPreviousName { get; set; }

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

        _journey.AuthenticationState.OnOfficialNameSet(
            OfficialFirstName!,
            OfficialLastName!,
            (HasPreviousNameOption)HasPreviousName!,
            HasPreviousName == HasPreviousNameOption.Yes ? PreviousOfficialFirstName : null,
            HasPreviousName == HasPreviousNameOption.Yes ? PreviousOfficialLastName : null);

        return await _journey.FindTrnAndContinue(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        OfficialFirstName ??= _journey.AuthenticationState.OfficialFirstName;
        OfficialLastName ??= _journey.AuthenticationState.OfficialLastName;
        PreviousOfficialFirstName ??= _journey.AuthenticationState.PreviousOfficialFirstName;
        PreviousOfficialLastName ??= _journey.AuthenticationState.PreviousOfficialLastName;
        HasPreviousName ??= _journey.AuthenticationState.HasPreviousName;
    }
}
