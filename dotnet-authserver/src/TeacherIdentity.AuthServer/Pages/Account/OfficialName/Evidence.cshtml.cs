using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Services.DqtEvidence;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialName;

[VerifyQueryParameterSignature]
[CheckOfficialNameChangeIsEnabled]
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

    [FromQuery(Name = "firstName")]
    public string? FirstName { get; set; }

    [FromQuery(Name = "middleName")]
    public string? MiddleName { get; set; }

    [FromQuery(Name = "lastName")]
    public string? LastName { get; set; }

    [FromQuery(Name = "preferredName")]
    public string? PreferredName { get; set; }

    [FromQuery(Name = "fromConfirmPage")]
    public bool FromConfirmPage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var fileName = EvidenceFile!.FileName;
        var fileId = GenerateFileId();

        return await TryUploadEvidence(fileId) ?
            Redirect(FromConfirmPage ?
                _linkGenerator.AccountOfficialNameConfirm(FirstName!, MiddleName, LastName!, fileName, fileId, PreferredName!, ClientRedirectInfo) :
                _linkGenerator.AccountOfficialNamePreferredName(FirstName!, MiddleName, LastName!, fileName, fileId, PreferredName, ClientRedirectInfo)) :
            this.PageWithErrors();
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (FirstName is null || LastName is null)
        {
            context.Result = BadRequest();
        }
    }
}
