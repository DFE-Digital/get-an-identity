using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[BindProperties]
public class EmailModel : PageModel
{
    [Display(Name = "Your email address")]
    [Required(ErrorMessage = "Enter your email address")]
    public string? Email { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        HttpContext.GetAuthenticationState().EmailAddress = Email;

        return Redirect(Url.EmailConfirmation());
    }
}
