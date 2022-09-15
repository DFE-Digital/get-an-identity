namespace TeacherIdentity.AuthServer.Services.DqtApi;

public class DqtApiClient : IDqtApiClient
{
    private readonly HttpClient _client;

    public DqtApiClient(HttpClient httpClient)
    {
        _client = httpClient;
    }

    public async Task SetTeacherIdentityInfo(DqtTeacherIdentityInfo info)
    {
        var response = await _client.PutAsJsonAsync($"/v2/teachers/teacher-identity/{info.Trn}", new { TsPersonId = info.UserId });
        response.EnsureSuccessStatusCode();
    }
}
