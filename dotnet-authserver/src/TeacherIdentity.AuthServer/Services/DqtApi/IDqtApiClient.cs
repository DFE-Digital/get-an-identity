namespace TeacherIdentity.AuthServer.Services.DqtApi;

public interface IDqtApiClient
{
    public Task<TeacherInfo?> GetTeacherByTrn(string trn);
}
