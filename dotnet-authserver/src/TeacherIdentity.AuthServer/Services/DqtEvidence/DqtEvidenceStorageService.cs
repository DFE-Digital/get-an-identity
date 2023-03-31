using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.UserImport;

namespace TeacherIdentity.AuthServer.Services.DqtEvidence;

public class DqtEvidenceStorageService : IDqtEvidenceStorageService
{
    private readonly string _dqtEvidenceContainerName;
    private readonly BlobServiceClient _blobServiceClient;

    public DqtEvidenceStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<DqtEvidenceOptions> dqtEvidenceOptions)
    {
        _blobServiceClient = blobServiceClient;
        _dqtEvidenceContainerName = dqtEvidenceOptions.Value.StorageContainerName;
    }

    public async Task Upload(IFormFile file, string blobName)
    {
        var blobContainerClient = await GetBlobContainerClient();
        var blobClient = blobContainerClient.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream);
    }

    private async Task<BlobContainerClient> GetBlobContainerClient()
    {
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_dqtEvidenceContainerName);
        await blobContainerClient.CreateIfNotExistsAsync();
        return blobContainerClient;
    }
}
