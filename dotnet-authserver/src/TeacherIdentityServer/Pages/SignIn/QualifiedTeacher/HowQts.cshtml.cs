using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentityServer.Pages.SignIn.QualifiedTeacher;

[BindProperties]
public class HowQtsModel : PageModel
{
    [Required(ErrorMessage = "xxx")]
    public bool? DidAUniversityScittOrSchoolAwardYourQts { get; set; }

    [Display(Name = "Where did you get your QTS?", Description = "Your university, SCITT, school or other training provider")]
    public string? WhereDidYouGetYourQts { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrEmpty(WhereDidYouGetYourQts) && DidAUniversityScittOrSchoolAwardYourQts == true)
        {
            ModelState.AddModelError(nameof(WhereDidYouGetYourQts), "xxx");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var authenticationState = HttpContext.GetAuthenticationState();
        authenticationState.DidAUniversityScittOrSchoolAwardQts = DidAUniversityScittOrSchoolAwardYourQts!.Value;
        authenticationState.QtsProviderName = WhereDidYouGetYourQts;

        return Redirect(Url.QualifiedTeacherCheckAnswers());
    }
}
