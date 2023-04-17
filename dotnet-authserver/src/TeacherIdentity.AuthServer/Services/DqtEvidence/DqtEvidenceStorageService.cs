using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

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
        var blobClient = await GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream);
    }

    public async Task<string> GetSasConnectionString(string blobName, int minutes)
    {
        var blobClient = await GetBlobClient(blobName);

        var sasBuilder = new BlobSasBuilder()
        {
            BlobContainerName = _dqtEvidenceContainerName,
            BlobName = blobName,
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(minutes),
            Protocol = SasProtocol.Https,
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);
        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }

    private async Task<BlobClient> GetBlobClient(string blobName)
    {
        var blobContainerClient = await GetBlobContainerClient();
        return blobContainerClient.GetBlobClient(blobName);
    }

    private async Task<BlobContainerClient> GetBlobContainerClient()
    {
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_dqtEvidenceContainerName);
        await blobContainerClient.CreateIfNotExistsAsync();
        return blobContainerClient;
    }
}
