using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.UserSearch;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[BindProperties]
public class DateOfBirthPage : PageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly IUserSearchService _userSearchService;

    public DateOfBirthPage(
        IdentityLinkGenerator linkGenerator,
        IUserSearchService userSearchService)
    {
        _linkGenerator = linkGenerator;
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
            return Redirect(_linkGenerator.RegisterAccountExists());
        }

        return authenticationState.OAuthState?.RequiresTrnLookup == true
            ? Redirect(_linkGenerator.RegisterHasNiNumber())
            : Redirect(_linkGenerator.RegisterCheckAnswers());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.PreferredNameSet)
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterName());
        }
    }
}
