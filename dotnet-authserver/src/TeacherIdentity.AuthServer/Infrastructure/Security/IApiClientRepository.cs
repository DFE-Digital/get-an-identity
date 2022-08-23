namespace TeacherIdentity.AuthServer.Infrastructure.Security;

public interface IApiClientRepository
{
    ApiClient? GetClientByClientId(string clientId);
    ApiClient? GetClientByKey(string apiKey);
}
