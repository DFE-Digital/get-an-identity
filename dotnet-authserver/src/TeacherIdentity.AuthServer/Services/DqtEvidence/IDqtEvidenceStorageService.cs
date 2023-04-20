namespace TeacherIdentity.AuthServer.Services.DqtEvidence;

public interface IDqtEvidenceStorageService
{
    Task<bool> TrySafeUpload(IFormFile file, string blobName);
    Task<string> GetSasConnectionString(string blobName, int minutes);
}
