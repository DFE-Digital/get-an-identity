using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentityServer.Pages.SignIn.QualifiedTeacher;

[BindProperties]
public class HaveNinoModel : PageModel
{
    [Required(ErrorMessage = "xxx")]
    public bool? DoYouHaveANationalInsuranceNumber { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        return Redirect(DoYouHaveANationalInsuranceNumber == true ? Url.QualifiedTeacherNino() : Url.QualifiedTeacherTrn());
    }
}
