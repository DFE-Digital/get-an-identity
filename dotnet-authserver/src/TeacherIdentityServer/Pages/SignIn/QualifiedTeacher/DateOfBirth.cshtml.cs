using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentityServer.Pages.SignIn.QualifiedTeacher;

[BindProperties]
public class DateOfBirthModel : PageModel
{
    [Display(Name = "Your date of birth")]
    [Required(ErrorMessage = "xxx")]
    public DateTime? DateOfBirth { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        HttpContext.GetAuthenticationState().DateOfBirth = DateOfBirth;

        return Redirect(Url.QualifiedTeacherHaveNino());
    }
}
