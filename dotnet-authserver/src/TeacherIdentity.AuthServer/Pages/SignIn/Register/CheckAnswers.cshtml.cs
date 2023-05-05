using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup))]
[CheckCanAccessStep(CurrentStep)]
public class CheckAnswers : PageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.CheckAnswers;

    private readonly IClock _clock;
    private readonly TrnLookupHelper _trnLookupHelper;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly SignInJourney _journey;
    private readonly CreateUserHelper _createUserHelper;

    public CheckAnswers(
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        TrnLookupHelper trnLookupHelper,
        SignInJourney journey, CreateUserHelper createUserHelper)
    {
        _dbContext = dbContext;
        _clock = clock;
        _trnLookupHelper = trnLookupHelper;
        _journey = journey;
        _createUserHelper = createUserHelper;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    public bool? RequiresTrnLookup => HttpContext.GetAuthenticationState().OAuthState?.RequiresTrnLookup;
    public string? EmailAddress => HttpContext.GetAuthenticationState().EmailAddress;
    public string? MobilePhoneNumber => HttpContext.GetAuthenticationState().MobileNumber;
    public string? FullName => HttpContext.GetAuthenticationState().GetPreferredName();
    public DateOnly? DateOfBirth => HttpContext.GetAuthenticationState().DateOfBirth;
    public bool? HasNationalInsuranceNumberSet => HttpContext.GetAuthenticationState().HasNationalInsuranceNumberSet;
    public string? NationalInsuranceNumber => HttpContext.GetAuthenticationState().NationalInsuranceNumber;
    public bool? AwardedQtsSet => HttpContext.GetAuthenticationState().AwardedQtsSet;
    public bool? AwardedQts => HttpContext.GetAuthenticationState().AwardedQts;
    public string? IttProviderName => HttpContext.GetAuthenticationState().IttProviderName;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        return await _journey.CreateUser(CurrentStep);
    }
}
