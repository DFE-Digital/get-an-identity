using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentityServer.Pages.SignIn.QualifiedTeacher;

[BindProperties]
public class NinoModel : PageModel
{
    [Display(Name = "What is your National Insurance number?")]
    [Required(ErrorMessage = "Enter your national insurance number")]
    public string? Nino { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        HttpContext.GetAuthenticationState().Nino = Nino;

        return Redirect(Url.QualifiedTeacherTrn());
    }
}
