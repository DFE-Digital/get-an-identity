using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

[CheckCanAccessStep(CurrentStep)]
public class NoMatch : PageModel
{
    private const string CurrentStep = LegacyTrnJourney.Steps.NoMatch;

    private readonly LegacyTrnJourney _journey;

    public NoMatch(LegacyTrnJourney journey)
    {
        _journey = journey;
    }

    [BindNever]
    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep)!;

    public string? EmailAddress => _journey.AuthenticationState.EmailAddress;
    public string? OfficialName => _journey.AuthenticationState.GetOfficialName();
    public string? PreviousOfficialName => _journey.AuthenticationState.GetPreviousOfficialName();
    public string? PreferredName => _journey.AuthenticationState.GetPreferredName();
    public DateOnly? DateOfBirth => _journey.AuthenticationState.DateOfBirth;
    public bool? HaveNationalInsuranceNumber => _journey.AuthenticationState.HasNationalInsuranceNumber;
    public string? NationalInsuranceNumber => _journey.AuthenticationState.NationalInsuranceNumber;
    public bool? AwardedQts => _journey.AuthenticationState.AwardedQts;
    public string? IttProviderName => _journey.AuthenticationState.IttProviderName;

    [BindProperty]
    [Display(Name = "Do you want to change something and try again?")]
    [Required(ErrorMessage = "Do you want to change something and try again?")]
    public bool? HasChangesToMake { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        if (HasChangesToMake == true)
        {
            return Redirect(_journey.GetPreviousStepUrl(LegacyTrnJourney.Steps.NoMatch)!);
        }

        return await _journey.CreateOrMatchUserWithTrn(CurrentStep);
    }
}
