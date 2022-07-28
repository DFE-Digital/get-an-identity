using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentityServer.Pages.SignIn.QualifiedTeacher;

[BindProperties]
public class HaveQtsModel : PageModel
{
    [Required(ErrorMessage = "xxx")]
    public bool? HaveYouBeenAwardedQts { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        HttpContext.GetAuthenticationState().HaveQts = HaveYouBeenAwardedQts!.Value;

        return Redirect(HaveYouBeenAwardedQts == true ? Url.QualifiedTeacherHowQts() : Url.QualifiedTeacherCheckAnswers());
    }
}
