using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[BindProperties]
public class EmailModel : PageModel
{
    private readonly IPinGenerator _pinGenerator;

    public EmailModel(IPinGenerator pinGenerator)
    {
        _pinGenerator = pinGenerator;
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

        var pin = await _pinGenerator.GenerateEmailConfirmationPin(Email!);
        // TODO Email the PIN

        return Redirect(Url.EmailConfirmation());
    }
}
