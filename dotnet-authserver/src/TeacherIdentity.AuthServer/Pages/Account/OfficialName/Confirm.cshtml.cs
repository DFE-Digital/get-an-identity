using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.DqtEvidence;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialName;

[VerifyQueryParameterSignature]
[CheckOfficialNameChangeIsEnabled]
public class Confirm : PageModel
{
    private const int SasTokenValidMinutes = 15;
    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly IDqtApiClient _dqtApiClient;
    private readonly IDqtEvidenceStorageService _dqtEvidenceStorage;

    public Confirm(
        IdentityLinkGenerator linkGenerator,
        IDqtApiClient dqtApiClient,
        IDqtEvidenceStorageService dqtEvidenceStorage)
    {
        _linkGenerator = linkGenerator;
        _dqtApiClient = dqtApiClient;
        _dqtEvidenceStorage = dqtEvidenceStorage;
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

    public async Task<IActionResult> OnPost()
    {
        var sasUri = await _dqtEvidenceStorage.GetSasConnectionString(FileName!, SasTokenValidMinutes);

        var teacherNameChangeRequest = new TeacherNameChangeRequest()
        {
            FirstName = FirstName!,
            MiddleName = MiddleName,
            LastName = LastName!,
            EvidenceFileName = FileName!,
            EvidenceFileUrl = sasUri,
            Trn = User.GetTrn()!
        };

        await _dqtApiClient.PostTeacherNameChange(teacherNameChangeRequest);

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
