namespace TeacherIdentity.AuthServer.Services.UserImport;

public interface IUserImportStorageService
{
    Task Upload(Stream stream, string targetFilename);

    Task<Stream> OpenReadStream(string filename);

    Task Archive(string filename);
}
