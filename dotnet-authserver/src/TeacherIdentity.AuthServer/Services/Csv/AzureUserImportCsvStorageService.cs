using Azure.Storage.Blobs;

namespace TeacherIdentity.AuthServer.Services.Csv;

public class AzureUserImportCsvStorageService : IUserImportCsvStorageService
{
    private const string PendingFolderName = "pending";
    private readonly string _connectionString;
    private readonly string _userImportsContainerName;

    public AzureUserImportCsvStorageService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DataProtectionBlobStorage") ??
           throw new Exception("Connection string DataProtectionBlobStorage is missing.");
        _userImportsContainerName = configuration.GetValue("UserImportsContainerName", "user-imports") ?? "user-imports";
    }

    public async Task Upload(Stream stream, string targetFilename)
    {
        var blobContainerClient = new BlobContainerClient(_connectionString, _userImportsContainerName);
        await blobContainerClient.CreateIfNotExistsAsync();

        var blobClient = blobContainerClient.GetBlobClient($"{PendingFolderName}/{targetFilename}");
        await blobClient.UploadAsync(stream);
    }
}
