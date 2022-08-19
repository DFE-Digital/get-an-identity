using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[BindProperties]
public class EmailModel : PageModel
{
    private readonly IEmailVerificationService _emailConfirmationService;

    public EmailModel(IEmailVerificationService emailConfirmationService)
    {
        _emailConfirmationService = emailConfirmationService;
    }

    [Display(Name = "Your email address")]
    [Required(ErrorMessage = "Enter your email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? Email { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.GetAuthenticationState().EmailAddress = Email;

        await _emailConfirmationService.GeneratePin(Email!);

        return Redirect(Url.EmailConfirmation());
    }
}
