using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.Account.Name;

[BindProperties]
public class Name : PageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;

    public Name(IdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    [BindNever]
    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [Display(Name = "First name", Description = "Or given names")]
    [Required(ErrorMessage = "Enter your first name")]
    [StringLength(200, ErrorMessage = "First name must be 200 characters or less")]
    public string? FirstName { get; set; }

    [Display(Name = "Middle name")]
    [StringLength(200, ErrorMessage = "Middle name must be 200 characters or less")]
    public string? MiddleName { get; set; }

    [Display(Name = "Last name", Description = "Or family names")]
    [Required(ErrorMessage = "Enter your last name")]
    [StringLength(200, ErrorMessage = "Last name must be 200 characters or less")]
    public string? LastName { get; set; }

    public void OnGet()
    {
        FirstName = User.GetFirstName();
        MiddleName = User.GetMiddleName();
        LastName = User.GetLastName();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        return Redirect(_linkGenerator.AccountNameConfirm(FirstName!, MiddleName, LastName!, ClientRedirectInfo));
    }
}
