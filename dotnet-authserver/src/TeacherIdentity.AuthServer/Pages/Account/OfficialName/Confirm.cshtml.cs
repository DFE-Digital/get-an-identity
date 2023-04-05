using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Infrastructure.Filters;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialName;

[VerifyQueryParameterSignature]
[CheckOfficialNameChangeIsEnabled]
public class Confirm : PageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;

    public Confirm(
        IdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [FromQuery(Name = "firstName")]
    public string? FirstName { get; set; }

    [FromQuery(Name = "middleName")]
    public string? MiddleName { get; set; }

    [FromQuery(Name = "lastName")]
    public string? LastName { get; set; }

    [FromQuery(Name = "fileId")]
    public string? FileId { get; set; }

    [FromQuery(Name = "fileName")]
    public string? FileName { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        TempData.SetFlashSuccess(
            "We’ve received your request to change your official name",
            "We’ll review it and get back to you within 5 working days.");

        return Redirect(_linkGenerator.Account(ClientRedirectInfo));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (FirstName is null || LastName is null || FileId is null || FileName is null)
        {
            context.Result = BadRequest();
        }
    }
}
