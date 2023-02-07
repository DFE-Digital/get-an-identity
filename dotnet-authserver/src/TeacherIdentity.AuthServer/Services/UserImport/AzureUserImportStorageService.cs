using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace TeacherIdentity.AuthServer.Services.UserImport;

public class AzureUserImportStorageService : IUserImportStorageService
{
    private const string PendingFolderName = "pending";
    private readonly string _userImportsContainerName;
    private readonly BlobServiceClient _blobServiceClient;

    public AzureUserImportStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<UserImportOptions> userImportOptions)
    {
        _blobServiceClient = blobServiceClient;
        _userImportsContainerName = userImportOptions.Value.StorageContainerName;
    }

    public async Task Upload(Stream stream, string targetFilename)
    {
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_userImportsContainerName);
        await blobContainerClient.CreateIfNotExistsAsync();

        var blobClient = blobContainerClient.GetBlobClient($"{PendingFolderName}/{targetFilename}");
        await blobClient.UploadAsync(stream);
    }
}
