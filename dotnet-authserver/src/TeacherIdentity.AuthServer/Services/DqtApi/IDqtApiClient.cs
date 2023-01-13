namespace TeacherIdentity.AuthServer.Services.DqtApi;

public interface IDqtApiClient
{
    Task<FindTeachersResponse> FindTeachers(FindTeachersRequest request, CancellationToken cancellationToken = default);
    public Task<TeacherInfo?> GetTeacherByTrn(string trn, CancellationToken cancellationToken = default);
}
