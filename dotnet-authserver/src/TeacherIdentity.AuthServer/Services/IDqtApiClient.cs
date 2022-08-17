using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Services;

public interface IDqtApiClient
{
    public Task SetTeacherIdentityInfo(DqtTeacherIdentityInfo info);
    public Task<DqtTeacherIdentityInfo?> GetTeacherIdentityInfo(Guid userId);
}
