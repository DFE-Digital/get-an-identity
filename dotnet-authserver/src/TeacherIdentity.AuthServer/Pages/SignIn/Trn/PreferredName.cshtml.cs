using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[BindProperties]
[CheckCanAccessStep(CurrentStep)]
public class PreferredName : PageModel
{
    private const string CurrentStep = LegacyTrnJourney.Steps.PreferredName;

    private readonly LegacyTrnJourney _journey;

    public PreferredName(LegacyTrnJourney journey)
    {
        _journey = journey;
    }

    [BindNever]
    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep)!;

    public string? OfficialFirstName => _journey.AuthenticationState.OfficialFirstName;

    public string? OfficialLastName => _journey.AuthenticationState.OfficialLastName;

    // Properties are set in the order that they are declared. Because the value of HasPreferredName
    // is used in the conditional RequiredIfTrue attribute, it should be set first.
    [Display(Name = " ")]
    [Required(ErrorMessage = "Select yes if this is your preferred name")]
    public bool? HasPreferredName { get; set; }

    [Display(Name = "Preferred first name")]
    [RequiredIfTrue(nameof(HasPreferredName), ErrorMessage = "Enter your preferred first name")]
    [StringLength(200, ErrorMessage = "Preferred first name must be 200 characters or less")]
    public string? PreferredFirstName { get; set; }

    [Display(Name = "Preferred last name")]
    [RequiredIfTrue(nameof(HasPreferredName), ErrorMessage = "Enter your preferred last name")]
    [StringLength(200, ErrorMessage = "Preferred last name must be 200 characters or less")]
    public string? PreferredLastName { get; set; }

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

        _journey.AuthenticationState.OnNameSet(
            HasPreferredName == true ? PreferredFirstName : null,
            null,
            HasPreferredName == true ? PreferredLastName : null);

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        PreferredFirstName ??= _journey.AuthenticationState.FirstName;
        PreferredLastName ??= _journey.AuthenticationState.LastName;

        HasPreferredName ??= !string.IsNullOrEmpty(PreferredFirstName) && !string.IsNullOrEmpty(PreferredLastName) ? true : null;
    }
}
