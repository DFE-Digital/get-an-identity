namespace TeacherIdentity.AuthServer.Infrastructure.Security;

public interface IApiClientRepository
{
    ApiClient? GetClientByKey(string apiKey);
}
