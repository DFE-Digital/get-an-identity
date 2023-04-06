using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialDateOfBirth;

[CheckOfficialDateOfBirthChangeIsEnabled]
public class Details : PageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;

    public Details(IdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [BindProperty]
    [Display(Name = "Date of birth", Description = "For example, 27 3 1987")]
    [Required(ErrorMessage = "Enter your date of birth")]
    [IsPastDate(typeof(DateOnly), ErrorMessage = "Your date of birth must be in the past")]
    public DateOnly? DateOfBirth { get; set; }

    private TeacherInfo? DqtUser { get; set; }

    public void OnGet()
    {
        DateOfBirth = DqtUser!.DateOfBirth;
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        if (DateOfBirth == DqtUser!.DateOfBirth)
        {
            ModelState.AddModelError(nameof(DateOfBirth), "The date entered matches your date of birth");
            return this.PageWithErrors();
        }

        return Redirect(_linkGenerator.AccountOfficialDateOfBirthEvidence((DateOnly)DateOfBirth!, ClientRedirectInfo));
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
}
