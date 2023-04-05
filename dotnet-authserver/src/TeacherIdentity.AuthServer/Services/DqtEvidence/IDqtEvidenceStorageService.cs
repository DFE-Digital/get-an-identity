namespace TeacherIdentity.AuthServer.Services.DqtEvidence;

public interface IDqtEvidenceStorageService
{
    Task Upload(IFormFile file, string targetFilename);
    Task<string> GetSasConnectionString(string blobName, int minutes);
}
