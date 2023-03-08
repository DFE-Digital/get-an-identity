using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class AccountExists : BaseExistingEmailPageModel
{
    private readonly IClock _clock;

    public AccountExists(
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        IUserVerificationService userVerificationService,
        IClock clock) :
        base(userVerificationService, linkGenerator, dbContext)
    {
        _clock = clock;
    }

    public string? Email => HttpContext.GetAuthenticationState().EmailAddress;
    public string? ExistingAccountEmail => HttpContext.GetAuthenticationState().ExistingAccountEmail;

    [BindProperty]
    [Display(Name = " ")]
    [Required(ErrorMessage = "Select yes if this is your account")]
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
        authenticationState.OnExistingAccountChosen((bool)IsUsersAccount!);

        if (IsUsersAccount == true)
        {
            var emailPinGenerationResult = await GenerateEmailPinForExistingEmail(ExistingAccountEmail!);
            return emailPinGenerationResult.Success
                ? Redirect(LinkGenerator.RegisterExistingAccountEmailConfirmation())
                : emailPinGenerationResult.Result!;
        }

        var user = await CreateUser();

        authenticationState.OnUserRegistered(user);
        await authenticationState.SignIn(HttpContext);

        return Redirect(authenticationState.GetNextHopUrl(LinkGenerator));
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

        DbContext.Users.Add(user);

        DbContext.AddEvent(new Events.UserRegisteredEvent()
        {
            ClientId = authenticationState.OAuthState?.ClientId,
            CreatedUtc = _clock.UtcNow,
            User = user
        });

        await DbContext.SaveChangesAsync();

        return user;
    }
}
