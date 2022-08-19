namespace TeacherIdentity.AuthServer.Services.DqtApi;

public interface IDqtApiClient
{
    public Task SetTeacherIdentityInfo(DqtTeacherIdentityInfo info);
    public Task<DqtTeacherIdentityInfo?> GetTeacherIdentityInfo(Guid userId);
}
