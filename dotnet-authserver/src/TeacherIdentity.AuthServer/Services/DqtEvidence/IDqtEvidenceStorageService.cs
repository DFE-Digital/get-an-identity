namespace TeacherIdentity.AuthServer.Services.DqtEvidence;

public interface IDqtEvidenceStorageService
{
    Task Upload(IFormFile file, string targetFilename);
}
