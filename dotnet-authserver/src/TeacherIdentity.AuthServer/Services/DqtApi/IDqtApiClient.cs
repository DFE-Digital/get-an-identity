namespace TeacherIdentity.AuthServer.Services.DqtApi;

public interface IDqtApiClient
{
    Task<FindTeachersResponse> FindTeachers(FindTeachersRequest request, CancellationToken cancellationToken = default);
    public Task<TeacherInfo?> GetTeacherByTrn(string trn, CancellationToken cancellationToken = default);
    public Task<GetIttProvidersResponse> GetIttProviders(CancellationToken cancellationToken = default);
    public Task PostTeacherNameChange(TeacherNameChangeRequest request, CancellationToken cancellationToken = default);
    public Task PostTeacherDateOfBirthChange(TeacherDateOfBirthChangeRequest request, CancellationToken cancellationToken = default);
}
