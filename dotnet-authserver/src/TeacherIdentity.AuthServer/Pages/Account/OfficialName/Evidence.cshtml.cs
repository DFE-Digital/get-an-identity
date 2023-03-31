using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Services.UserImport;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialName;

[VerifyQueryParameterSignature]
[OfficialNameChangeEnabled]
public class Evidence : PageModel
{
    public const int MaxFileSizeMb = 3;

    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly IDqtEvidenceStorageService _dqtEvidenceStorage;

    public Evidence(
        IdentityLinkGenerator linkGenerator,
        IDqtEvidenceStorageService dqtEvidenceStorage)
    {
        _linkGenerator = linkGenerator;
        _dqtEvidenceStorage = dqtEvidenceStorage;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [FromQuery(Name = "firstName")]
    public string? FirstName { get; set; }

    [FromQuery(Name = "middleName")]
    public string? MiddleName { get; set; }

    [FromQuery(Name = "lastName")]
    public string? LastName { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Select a file")]
    [FileExtensions(".csv,.jpg,.jpeg", ErrorMessage = "The selected file must be a CSV or JPEG")]
    [FileSize(MaxFileSizeMb * 1024 * 1024, ErrorMessage = "The selected file must be smaller than 3MB")]
    public IFormFile? EvidenceFile { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var fileId = Guid.NewGuid();
        var fileName = $"{User.GetUserId()}/{fileId}";

        await _dqtEvidenceStorage.Upload(EvidenceFile!, fileName);

        return Redirect(_linkGenerator.AccountOfficialNameConfirm(FirstName!, MiddleName ?? String.Empty, LastName!, fileId.ToString(), fileName, ClientRedirectInfo));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (FirstName is null || LastName is null)
        {
            context.Result = BadRequest();
        }
    }
}
