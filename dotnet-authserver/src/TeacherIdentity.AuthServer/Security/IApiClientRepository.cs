namespace TeacherIdentity.AuthServer.Security;

public interface IApiClientRepository
{
    ApiClient? GetClientByKey(string apiKey);
}
