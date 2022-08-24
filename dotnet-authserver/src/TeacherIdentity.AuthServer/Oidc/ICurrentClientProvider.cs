using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Oidc;

public interface ICurrentClientProvider
{
    Task<Application?> GetCurrentClient();
}
