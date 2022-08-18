using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Services.DqtApi;

public class DqtApiClient : IDqtApiClient
{
    private IConfiguration _configuration;
    private HttpClient _client;

    public DqtApiClient(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _client = httpClient;
    }

    public async Task<DqtTeacherIdentityInfo?> GetTeacherIdentityInfo(Guid userId)
    {
        var response = await _client.GetFromJsonAsync<DqtTeacherIdentityInfo?>($"/v2/teachers/teacher-identity?tsPersonId={userId}");
        return response;
    }

    public async Task SetTeacherIdentityInfo(DqtTeacherIdentityInfo info)
    {
        var response = await _client.PutAsJsonAsync($"/v2/teachers/teacher-identity/{info.Trn}", new { info.TsPersonId });
        response.EnsureSuccessStatusCode();
    }
}
