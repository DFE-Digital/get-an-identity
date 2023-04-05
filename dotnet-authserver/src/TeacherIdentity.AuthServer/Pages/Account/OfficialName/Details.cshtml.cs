using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialName;

[CheckOfficialNameChangeIsEnabled]
public class Details : PageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;

    public Details(IdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [BindProperty]
    [Display(Name = "First name", Description = "Or given names")]
    [Required(ErrorMessage = "Enter your first name")]
    [StringLength(100, ErrorMessage = "First name must be 100 characters or less")]
    public string? FirstName { get; set; }

    [BindProperty]
    [Display(Name = "Middle name (optional)")]
    [StringLength(100, ErrorMessage = "Middle name must be 100 characters or less")]
    public string? MiddleName { get; set; }

    [BindProperty]
    [Display(Name = "Last name", Description = "Or family names")]
    [Required(ErrorMessage = "Enter your last name")]
    [StringLength(100, ErrorMessage = "Last name must be 100 characters or less")]
    public string? LastName { get; set; }

    private TeacherInfo? DqtUser { get; set; }

    public void OnGet()
    {
        FirstName = DqtUser!.FirstName;
        MiddleName = DqtUser.MiddleName;
        LastName = DqtUser.LastName;
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        if (NamesUnchanged())
        {
            ModelState.AddModelError(nameof(FirstName), "The name entered matches your official name");
            return this.PageWithErrors();
        }

        return Redirect(_linkGenerator.AccountOfficialNameEvidence(FirstName!, MiddleName ?? String.Empty, LastName!, ClientRedirectInfo));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (context.HttpContext.Items["DqtUser"] is TeacherInfo dqtUser)
        {
            DqtUser = dqtUser;
        }
        else
        {
            context.Result = new BadRequestResult();
        }
    }

    private bool NamesUnchanged()
    {
        return FirstName == DqtUser!.FirstName &&
               MiddleName == DqtUser.MiddleName &&
               LastName == DqtUser.LastName;
    }
}
