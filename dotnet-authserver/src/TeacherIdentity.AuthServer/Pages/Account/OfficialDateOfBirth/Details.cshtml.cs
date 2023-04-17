using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialDateOfBirth;

[VerifyQueryParameterSignature]
[CheckOfficialDateOfBirthChangeIsEnabled]
public class Details : PageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;

    public Details(IdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [BindProperty(SupportsGet = true)]
    [Display(Name = "Date of birth", Description = "For example, 27 3 1987")]
    [Required(ErrorMessage = "Enter your date of birth")]
    [IsPastDate(typeof(DateOnly), ErrorMessage = "Your date of birth must be in the past")]
    public DateOnly? DateOfBirth { get; set; }

    [FromQuery(Name = "fileName")]
    public string? FileName { get; set; }

    [FromQuery(Name = "fileId")]
    public string? FileId { get; set; }

    [FromQuery(Name = "fromConfirmPage")]
    public bool FromConfirmPage { get; set; }

    private TeacherInfo? DqtUser { get; set; }

    public void OnGet()
    {
        // We may have been passed a value e.g. when we came from the Confirm page.
        // If not, default to the current DQT value and ensure we don't show an 'Enter your date of birth' error.
        DateOfBirth ??= DqtUser!.DateOfBirth;
        ModelState.Clear();
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

        return Redirect(FromConfirmPage && FileName is not null && FileId is not null ?
            _linkGenerator.AccountOfficialDateOfBirthConfirm((DateOnly)DateOfBirth!, FileName, FileId, ClientRedirectInfo) :
            _linkGenerator.AccountOfficialDateOfBirthEvidence((DateOnly)DateOfBirth!, ClientRedirectInfo));
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
