using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Infrastructure.Filters;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialDateOfBirth;

[VerifyQueryParameterSignature]
[CheckOfficialDateOfBirthChangeIsEnabled]
public class Confirm : PageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;

    public Confirm(IdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [FromQuery(Name = "dateOfBirth")]
    public DateOnly? DateOfBirth { get; set; }

    [FromQuery(Name = "fileName")]
    public string? FileName { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        TempData.SetFlashSuccess(
        "We’ve received your request to change your date of birth",
        "We’ll review it and get back to you within 5 working days.");

        return Redirect(_linkGenerator.Account(ClientRedirectInfo));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (DateOfBirth is null || FileName is null)
        {
            context.Result = BadRequest();
        }
    }
}
