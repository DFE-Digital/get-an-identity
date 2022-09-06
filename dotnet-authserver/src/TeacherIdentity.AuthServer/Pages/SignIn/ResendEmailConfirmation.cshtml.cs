using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[BindProperties]
public class ResendEmailConfirmationModel : PageModel
{
    private readonly IEmailVerificationService _emailVerificationService;

    public ResendEmailConfirmationModel(IEmailVerificationService emailVerificationService)
    {
        _emailVerificationService = emailVerificationService;
    }

    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Enter your email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? Email { get; set; }

    public void OnGet()
    {
        Email = HttpContext.GetAuthenticationState().EmailAddress;
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.GetAuthenticationState().EmailAddress = Email;

        await _emailVerificationService.GeneratePin(Email!);

        return Redirect(Url.EmailConfirmation());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (authenticationState.EmailAddress is null)
        {
            context.Result = Redirect(Url.Email());
        }
        else if (authenticationState.EmailAddressVerified)
        {
            context.Result = Redirect(authenticationState.GetNextHopUrl(Url));
        }
    }
}
