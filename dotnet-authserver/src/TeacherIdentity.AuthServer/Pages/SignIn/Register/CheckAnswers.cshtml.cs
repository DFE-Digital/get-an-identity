using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class CheckAnswers : PageModel
{
    private readonly IClock _clock;
    private readonly TrnLookupHelper _trnLookupHelper;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IdentityLinkGenerator _linkGenerator;

    public CheckAnswers(
        IdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        TrnLookupHelper trnLookupHelper)
    {
        _linkGenerator = linkGenerator;
        _dbContext = dbContext;
        _clock = clock;
        _trnLookupHelper = trnLookupHelper;
    }

    public string BackLink => HttpContext.GetAuthenticationState().OAuthState?.RequiresTrnLookup == true
        ? _linkGenerator.RegisterIttProvider()
        : _linkGenerator.RegisterDateOfBirth();

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
        return await TryCreateUser();
    }

    protected async Task<IActionResult> TryCreateUser()
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Created = _clock.UtcNow,
            DateOfBirth = authenticationState.DateOfBirth,
            EmailAddress = authenticationState.EmailAddress!,
            MobileNumber = authenticationState.MobileNumber,
            NormalizedMobileNumber = MobileNumber.Parse(authenticationState.MobileNumber!),
            FirstName = authenticationState.FirstName!,
            LastName = authenticationState.LastName!,
            Updated = _clock.UtcNow,
            UserId = userId,
            UserType = UserType.Default,
            LastSignedIn = _clock.UtcNow,
            RegisteredWithClientId = authenticationState.OAuthState?.ClientId,
        };

        _dbContext.Users.Add(user);

        _dbContext.AddEvent(new Events.UserRegisteredEvent()
        {
            ClientId = authenticationState.OAuthState?.ClientId,
            CreatedUtc = _clock.UtcNow,
            User = user
        });

        await _dbContext.SaveChangesAsync();

        authenticationState.OnUserRegistered(user);
        await authenticationState.SignIn(HttpContext);

        return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
    }
}
