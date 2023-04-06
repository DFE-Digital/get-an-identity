using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.DqtEvidence;

namespace TeacherIdentity.AuthServer.Pages.Account;

public abstract class BaseEvidencePage : PageModel
{
    public const int MaxFileSizeMb = 3;

    private readonly IDqtEvidenceStorageService _dqtEvidenceStorage;

    protected BaseEvidencePage(IDqtEvidenceStorageService dqtEvidenceStorage)
    {
        _dqtEvidenceStorage = dqtEvidenceStorage;
    }

    [BindProperty]
    [Required(ErrorMessage = "Select a file")]
    [FileExtensions(".pdf", ".jpg", ".jpeg", ErrorMessage = "The selected file must be a PDF or JPEG")]
    [FileSize(MaxFileSizeMb * 1024 * 1024, ErrorMessage = "The selected file must be smaller than 3MB")]
    public IFormFile? EvidenceFile { get; set; }

    protected async Task<string> UploadEvidence()
    {
        var fileId = Guid.NewGuid();
        var extension = Path.GetExtension(EvidenceFile!.FileName);

        var fileName = $"{User.GetUserId()}/{fileId}{extension}";
        await _dqtEvidenceStorage.Upload(EvidenceFile!, fileName);

        return fileName;
    }
}
