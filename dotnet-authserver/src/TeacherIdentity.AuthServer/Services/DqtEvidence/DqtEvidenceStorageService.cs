using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace TeacherIdentity.AuthServer.Services.DqtEvidence;

public class DqtEvidenceStorageService : IDqtEvidenceStorageService
{
    private const string MicrosoftDefenderMalwareScanKey = "Malware Scanning scan result";
    private const string MicrosoftDefenderMalwareScanSuccessValue = "No threats found";

    private const int PollingTimeoutMs = 30000;
    private const int InitialPollingDelayMs = 750;
    private const int PollingPeriodMs = 250;

    private readonly string _dqtEvidenceContainerName;
    private readonly BlobServiceClient _blobServiceClient;

    public DqtEvidenceStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<DqtEvidenceOptions> dqtEvidenceOptions)
    {
        _blobServiceClient = blobServiceClient;
        _dqtEvidenceContainerName = dqtEvidenceOptions.Value.StorageContainerName;
    }

    public async Task<bool> TrySafeUpload(IFormFile file, string blobName)
    {
        var blobClient = await GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream);

        using var cancellationToken = new CancellationTokenSource();
        cancellationToken.CancelAfter(PollingTimeoutMs);

        var malwareScanResult = await PollForMalwareScanResult(blobClient, cancellationToken.Token);
        return malwareScanResult == MicrosoftDefenderMalwareScanSuccessValue;
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

    private async Task<string?> PollForMalwareScanResult(BlobClient blobClient, CancellationToken cancellationToken)
    {
        await Task.Delay(InitialPollingDelayMs);

        string? malwareScanResult;
        var malwareScanComplete = false;

        do
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException();
            }

            await Task.Delay(PollingPeriodMs);

            var blobTags = await blobClient.GetTagsAsync();
            if (blobTags.Value.Tags.TryGetValue(MicrosoftDefenderMalwareScanKey, out malwareScanResult))
            {
                malwareScanComplete = true;
            }
        } while (!malwareScanComplete);

        return malwareScanResult;
    }
}
