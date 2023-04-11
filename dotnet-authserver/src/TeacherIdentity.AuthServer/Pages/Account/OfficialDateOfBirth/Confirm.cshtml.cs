using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.DqtEvidence;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialDateOfBirth;

[VerifyQueryParameterSignature]
[CheckOfficialDateOfBirthChangeIsEnabled]
public class Confirm : PageModel
{
    private const int SasTokenValidMinutes = 15;
    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly IDqtEvidenceStorageService _dqtEvidenceStorage;
    private readonly IDqtApiClient _dqtApiClient;

    public Confirm(
        IdentityLinkGenerator linkGenerator,
        IDqtEvidenceStorageService dqtEvidenceStorage,
        IDqtApiClient dqtApiClient)
    {
        _linkGenerator = linkGenerator;
        _dqtEvidenceStorage = dqtEvidenceStorage;
        _dqtApiClient = dqtApiClient;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [FromQuery(Name = "dateOfBirth")]
    public DateOnly? DateOfBirth { get; set; }

    [FromQuery(Name = "fileName")]
    public string? FileName { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        var sasUri = await _dqtEvidenceStorage.GetSasConnectionString(FileName!, SasTokenValidMinutes);

        var teacherDobChangeRequest = new TeacherDateOfBirthChangeRequest()
        {
            DateOfBirth = DateOfBirth!.Value,
            EvidenceFileName = FileName!,
            EvidenceFileUrl = sasUri,
            Trn = User.GetTrn()!
        };

        await _dqtApiClient.PostTeacherDateOfBirthChange(teacherDobChangeRequest);

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
