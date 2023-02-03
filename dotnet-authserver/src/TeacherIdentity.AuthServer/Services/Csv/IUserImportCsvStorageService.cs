namespace TeacherIdentity.AuthServer.Services.Csv;

public interface IUserImportCsvStorageService
{
    Task Upload(Stream stream, string targetFilename);
}
