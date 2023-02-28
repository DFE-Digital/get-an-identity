using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserSearch;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[BindProperties]
public class DateOfBirthPage : PageModel
{
    IIdentityLinkGenerator _linkGenerator;
    private TeacherIdentityServerDbContext _dbContext;
    private IClock _clock;
    private readonly IUserSearchService _userSearchService;

    public DateOfBirthPage(
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        IUserSearchService userSearchService)
    {
        _linkGenerator = linkGenerator;
        _dbContext = dbContext;
        _clock = clock;
        _userSearchService = userSearchService;
    }

    [Display(Name = "Your date of birth", Description = "For example, 27 3 1987")]
    [Required(ErrorMessage = "Enter your date of birth")]
    [IsPastDate(typeof(DateOnly), ErrorMessage = "Your date of birth must be in the past")]
    public DateOnly? DateOfBirth { get; set; }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var authenticationState = HttpContext.GetAuthenticationState();
        authenticationState.OnDateOfBirthSet((DateOnly)DateOfBirth!);

        var users = await _userSearchService.FindUsers(
            authenticationState.FirstName!,
            authenticationState.LastName!,
            (DateOnly)DateOfBirth!);

        if (users.Length > 0)
        {
            authenticationState.OnExistingAccountFound(users[0]);
            return Redirect(_linkGenerator.RegisterCheckAccount());
        }

        var user = await CreateUser();

        authenticationState.OnUserRegistered(user);
        await authenticationState.SignIn(HttpContext);

        return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
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
