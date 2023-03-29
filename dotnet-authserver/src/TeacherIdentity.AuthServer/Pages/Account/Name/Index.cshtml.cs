using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.Account.Name;

[BindProperties]
public class Name : PageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly ProtectedStringFactory _protectedStringFactory;

    public Name(IdentityLinkGenerator linkGenerator, ProtectedStringFactory protectedStringFactory)
    {
        _linkGenerator = linkGenerator;
        _protectedStringFactory = protectedStringFactory;
    }

    [BindNever]
    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [Display(Name = "First name", Description = "Or given names")]
    [Required(ErrorMessage = "Enter your first name")]
    [StringLength(200, ErrorMessage = "First name must be 200 characters or less")]
    public string? FirstName { get; set; }

    [Display(Name = "Last name", Description = "Or family names")]
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

        var protectedFirstName = _protectedStringFactory.CreateFromPlainValue(FirstName!);
        var protectedLastName = _protectedStringFactory.CreateFromPlainValue(LastName!);

        return Redirect(_linkGenerator.AccountNameConfirm(protectedFirstName, protectedLastName, ClientRedirectInfo));
    }
}
