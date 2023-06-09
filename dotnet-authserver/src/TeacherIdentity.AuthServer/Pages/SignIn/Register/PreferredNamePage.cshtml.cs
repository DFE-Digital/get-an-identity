using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup), typeof(TrnTokenSignInJourney))]
[CheckCanAccessStep(CurrentStep)]
public class PreferredNamePage : PageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.PreferredName;

    private SignInJourney _journey;

    public PreferredNamePage(SignInJourney journey)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);
    public bool HasMiddleName => _journey.AuthenticationState.HasMiddleName;
    public string ExistingName(bool includeMiddleName) => _journey.AuthenticationState.GetName(includeMiddleName)!;


    [BindProperty]
    [Display(Name = " ")]
    [Required(ErrorMessage = "Select which name to use")]
    public PreferredNameOption? HasPreferredName { get; set; }

    [BindProperty]
    [Display(Name = "Your preferred name")]
    [RequiredIfTrue(nameof(HasPreferredName), ErrorMessage = "Enter your preferred name")]
    [StringLengthIfTrue(nameof(HasPreferredName), 200, ErrorMessage = "Preferred name must be 200 characters or less")]
    public string? PreferredName { get; set; }

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

        var preferredName = HasPreferredName switch
        {
            PreferredNameOption.ExistingFullName => ExistingName(includeMiddleName: true),
            PreferredNameOption.ExistingName => ExistingName(includeMiddleName: false),
            PreferredNameOption.PreferredName => PreferredName,
            _ => throw new ArgumentOutOfRangeException(nameof(HasPreferredName), HasPreferredName, "Invalid preferred name option chosen")
        };

        HttpContext.GetAuthenticationState().OnPreferredNameSet(preferredName!);

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        var authState = _journey.AuthenticationState;

        if (string.IsNullOrEmpty(authState.PreferredName))
        {
            return;
        }

        if (authState.HasMiddleName && authState.PreferredName == authState.GetName(includeMiddleName: true))
        {
            HasPreferredName = PreferredNameOption.ExistingFullName;
        }
        else if (authState.PreferredName == authState.GetName(includeMiddleName: false))
        {
            HasPreferredName = PreferredNameOption.ExistingName;
        }
        else
        {
            HasPreferredName = PreferredNameOption.PreferredName;
            PreferredName = authState.PreferredName;
        }
    }

    public enum PreferredNameOption
    {
        ExistingFullName,
        ExistingName,
        PreferredName
    }
}
