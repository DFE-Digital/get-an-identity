using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeacherIdentity.AuthServer.Pages.Account.Name;

[BindProperties]
public class Name : PageModel
{
    private IIdentityLinkGenerator _linkGenerator;
    private readonly ProtectedStringFactory _protectedStringFactory;

    public Name(IIdentityLinkGenerator linkGenerator, ProtectedStringFactory protectedStringFactory)
    {
        _linkGenerator = linkGenerator;
        _protectedStringFactory = protectedStringFactory;
    }

    [Display(Name = "First name", Description = "Or given names")]
    [Required(ErrorMessage = "Enter your first name")]
    [StringLength(200, ErrorMessage = "First name must be 200 characters or less")]
    public string? FirstName { get; set; }

    [Display(Name = "Last name", Description = "Or family names")]
    [Required(ErrorMessage = "Enter your last name")]
    [StringLength(200, ErrorMessage = "Last name must be 200 characters or less")]
    public string? LastName { get; set; }

    [FromQuery(Name = "returnPath")]
    public string? ReturnPath { get; set; }
    public string? SafeReturnPath { get; set; }

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

        return Redirect(_linkGenerator.AccountNameConfirm(protectedFirstName, protectedLastName, ReturnPath));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        SafeReturnPath = !string.IsNullOrEmpty(ReturnPath) && Url.IsLocalUrl(ReturnPath) ? ReturnPath : "/account";
    }
}
