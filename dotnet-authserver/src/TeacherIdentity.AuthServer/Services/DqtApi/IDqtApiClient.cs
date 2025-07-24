namespace TeacherIdentity.AuthServer.Services.DqtApi;

public interface IDqtApiClient
{
    Task<FindTeachersResponse> FindTeachers(FindTeachersRequest request, CancellationToken cancellationToken = default);
    public Task<TeacherInfo?> GetTeacherByTrn(string trn, CancellationToken cancellationToken = default);
    public Task<GetIttProvidersResponse> GetIttProviders(CancellationToken cancellationToken = default);
    public Task PostTeacherNameChange(string trn, TeacherNameChangeRequest request, CancellationToken cancellationToken = default);
    public Task PostTeacherDateOfBirthChange(string trn, TeacherDateOfBirthChangeRequest request, CancellationToken cancellationToken = default);
}
