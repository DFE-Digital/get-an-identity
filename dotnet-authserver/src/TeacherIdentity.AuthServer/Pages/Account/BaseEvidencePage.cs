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
    [FileExtensions(".pdf", ".jpg", ".jpeg", ".png", ErrorMessage = "The selected file must be an image or a PDF")]
    [FileSize(MaxFileSizeMb * 1024 * 1024, ErrorMessage = "The selected file must be smaller than 3MB")]
    public IFormFile? EvidenceFile { get; set; }

    protected string GenerateFileId()
    {
        var guid = Guid.NewGuid();
        var extension = Path.GetExtension(EvidenceFile!.FileName);

        return $"{User.GetUserId()}/{guid}{extension}";
    }

    protected async Task<bool> TryUploadEvidence(string fileId)
    {
        if (!await _dqtEvidenceStorage.TrySafeUpload(EvidenceFile!, fileId))
        {
            ModelState.AddModelError(nameof(EvidenceFile), "The selected file contains a virus");
            return false;
        }

        return true;
    }
}
