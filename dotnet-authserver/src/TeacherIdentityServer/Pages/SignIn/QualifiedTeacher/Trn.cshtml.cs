using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentityServer.Pages.SignIn.QualifiedTeacher;

[BindProperties]
public class TrnModel : PageModel
{
    [Required(ErrorMessage = "xxxx")]
    public bool? DoYouKnowYourTrn { get; set; }

    [Display(Name = "What is your TRN?")]
    public string? WhatIsYourTrn { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (DoYouKnowYourTrn == true && string.IsNullOrEmpty(WhatIsYourTrn))
        {
            ModelState.AddModelError(nameof(WhatIsYourTrn), "xxx");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        HttpContext.GetAuthenticationState().Trn = WhatIsYourTrn;

        return Redirect(Url.QualifiedTeacherHaveQts());
    }
}
