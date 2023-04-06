using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Services.DqtEvidence;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialDateOfBirth;

[VerifyQueryParameterSignature]
[CheckOfficialDateOfBirthChangeIsEnabled]
public class Evidence : BaseEvidencePage
{
    private readonly IdentityLinkGenerator _linkGenerator;

    public Evidence(
        IdentityLinkGenerator linkGenerator,
        IDqtEvidenceStorageService dqtEvidenceStorage)
        : base(dqtEvidenceStorage)
    {
        _linkGenerator = linkGenerator;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [FromQuery(Name = "dateOfBirth")]
    public DateOnly? DateOfBirth { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var fileName = await UploadEvidence();
        return Redirect(_linkGenerator.AccountOfficialDateOfBirthConfirm((DateOnly)DateOfBirth!, fileName, ClientRedirectInfo));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (DateOfBirth is null)
        {
            context.Result = BadRequest();
        }
    }
}
