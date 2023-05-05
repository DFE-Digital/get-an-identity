using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup))]
[CheckCanAccessStep(CurrentStep)]
public class CheckAnswers : PageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.CheckAnswers;

    private readonly SignInJourney _journey;

    public CheckAnswers(SignInJourney journey)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    public bool? RequiresTrnLookup => _journey.AuthenticationState.UserRequirements.RequiresTrnLookup();
    public string? EmailAddress => _journey.AuthenticationState.EmailAddress;
    public string? MobilePhoneNumber => _journey.AuthenticationState.MobileNumber;
    public string? FullName => _journey.AuthenticationState.GetPreferredName();
    public DateOnly? DateOfBirth => _journey.AuthenticationState.DateOfBirth;
    public bool? HasNationalInsuranceNumberSet => _journey.AuthenticationState.HasNationalInsuranceNumberSet;
    public string? NationalInsuranceNumber => _journey.AuthenticationState.NationalInsuranceNumber;
    public bool? AwardedQtsSet => _journey.AuthenticationState.AwardedQtsSet;
    public bool? AwardedQts => _journey.AuthenticationState.AwardedQts;
    public string? IttProviderName => _journey.AuthenticationState.IttProviderName;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        return await _journey.CreateUser(CurrentStep);
    }
}
