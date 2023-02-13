using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Options;

namespace TeacherIdentity.AuthServer.Services.UserImport;

public class BlobStorageUserImportStorageService : IUserImportStorageService
{
    private const string PendingFolderName = "pending";
    private const string ProcessedFolderName = "processed";
    private readonly string _userImportsContainerName;
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorageUserImportStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<UserImportOptions> userImportOptions)
    {
        _blobServiceClient = blobServiceClient;
        _userImportsContainerName = userImportOptions.Value.StorageContainerName;
    }

    public async Task Upload(Stream stream, string targetFilename)
    {
        var blobContainerClient = await GetBlobContainerClient();
        var blobClient = blobContainerClient.GetBlobClient($"{PendingFolderName}/{targetFilename}");
        await blobClient.UploadAsync(stream);
    }

    public async Task<Stream> OpenReadStream(string filename)
    {
        var blobContainerClient = await GetBlobContainerClient();
        var blobClient = blobContainerClient.GetBlobClient($"{PendingFolderName}/{filename}");
        return await blobClient.OpenReadAsync();
    }

    public async Task Archive(string filename)
    {
        var blobContainerClient = await GetBlobContainerClient();
        var sourceBlobClient = blobContainerClient.GetBlobClient($"{PendingFolderName}/{filename}");
        if (await sourceBlobClient.ExistsAsync())
        {
            var targetFilename = $"{ProcessedFolderName}/{filename}";

            // Acquire a lease to prevent another client modifying the source blob
            var lease = sourceBlobClient.GetBlobLeaseClient();
            await lease.AcquireAsync(TimeSpan.FromSeconds(60));

            var targetBlobClient = blobContainerClient.GetBlobClient(targetFilename);
            var copyOperation = await targetBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
            await copyOperation.WaitForCompletionAsync();

            // Release the lease
            var sourceProperties = await sourceBlobClient.GetPropertiesAsync();
            if (sourceProperties.Value.LeaseState == LeaseState.Leased)
            {
                await lease.ReleaseAsync();
            }

            // Now remove the original blob
            await sourceBlobClient.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }
    }

    private async Task<BlobContainerClient> GetBlobContainerClient()
    {
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_userImportsContainerName);
        await blobContainerClient.CreateIfNotExistsAsync();
        return blobContainerClient;
    }
}
