using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static TeacherIdentity.AuthServer.AuthenticationState;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[BindProperties]
[RequireAuthenticationMilestone(AuthenticationMilestone.EmailVerified)]
public class Name : PageModel
{
    private IIdentityLinkGenerator _linkGenerator;

    public Name(IIdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    [Display(Name = "First name")]
    [Required(ErrorMessage = "Enter your first name")]
    [StringLength(200, ErrorMessage = "First name must be 200 characters or less")]
    public string? FirstName { get; set; }

    [Display(Name = "Last name")]
    [Required(ErrorMessage = "Enter your last name")]
    [StringLength(200, ErrorMessage = "Last name must be 200 characters or less")]
    public string? LastName { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.GetAuthenticationState().OnNameSet(FirstName!, LastName!);

        return Redirect(_linkGenerator.RegisterDateOfBirth());
    }
}
