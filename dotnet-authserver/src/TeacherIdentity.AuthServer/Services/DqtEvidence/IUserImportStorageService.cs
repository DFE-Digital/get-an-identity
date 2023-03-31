namespace TeacherIdentity.AuthServer.Services.UserImport;

public interface IDqtEvidenceStorageService
{
    Task Upload(IFormFile file, string targetFilename);
}
