namespace TeacherIdentity.AuthServer.Services.UserImport;

public interface IUserImportProcessor
{
    Task Process(Guid userImportJobId);
}
