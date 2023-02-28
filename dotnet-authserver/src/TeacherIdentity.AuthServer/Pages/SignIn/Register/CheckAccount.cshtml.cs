using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class CheckAccount : PageModel
{
    private readonly IIdentityLinkGenerator _linkGenerator;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IUserVerificationService _userVerificationService;
    private readonly IClock _clock;

    public CheckAccount(
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        IUserVerificationService userVerificationService, IClock clock)
    {
        _linkGenerator = linkGenerator;
        _dbContext = dbContext;
        _userVerificationService = userVerificationService;
        _clock = clock;
    }

    public string? Email => HttpContext.GetAuthenticationState().EmailAddress;
    public string? ExistingEmail => HttpContext.GetAuthenticationState().ExistingAccountEmail;

    [BindProperty]
    [Display(Name = " ")]
    [Required(ErrorMessage = "Content TBD")]
    public bool? IsUsersAccount { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var authenticationState = HttpContext.GetAuthenticationState();
        authenticationState.OnExistingAccountConfirmed((bool)IsUsersAccount!);

        if (IsUsersAccount == true)
        {
            await TryGenerateEmailPinForEmail(ExistingEmail!);
            return Redirect(_linkGenerator.RegisterConfirmExistingAccount());
        }

        var user = await CreateUser();

        authenticationState.OnUserRegistered(user);
        await authenticationState.SignIn(HttpContext);

        return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
    }

    private async Task<EmailValidationResult> TryGenerateEmailPinForEmail(string email)
    {
        var pinGenerationResult = await _userVerificationService.GenerateEmailPin(email);

        switch (pinGenerationResult.FailedReasons)
        {
            case PinGenerationFailedReasons.None:
                return EmailValidationResult.Success();

            case PinGenerationFailedReasons.RateLimitExceeded:
                return EmailValidationResult.Failed(new ViewResult()
                {
                    StatusCode = 429,
                    ViewName = "TooManyRequests"
                });

            case PinGenerationFailedReasons.InvalidAddress:
                ModelState.AddModelError(nameof(email), "Enter a valid email address");
                return EmailValidationResult.Failed(this.PageWithErrors());

            default:
                throw new NotImplementedException($"Unknown {nameof(PinGenerationFailedReasons)}: '{pinGenerationResult.FailedReasons}'.");
        }
    }

    private async Task<User> CreateUser()
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        var userId = Guid.NewGuid();
        var user = new User()
        {
            Created = _clock.UtcNow,
            DateOfBirth = authenticationState.DateOfBirth,
            EmailAddress = authenticationState.EmailAddress!,
            MobileNumber = PhoneHelper.FormatMobileNumber(authenticationState.MobileNumber!),
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

        return user;
    }
}
