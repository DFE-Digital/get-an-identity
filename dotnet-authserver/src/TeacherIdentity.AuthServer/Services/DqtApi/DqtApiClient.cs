namespace TeacherIdentity.AuthServer.Services.DqtApi;

public class DqtApiClient : IDqtApiClient
{
    private readonly HttpClient _client;

    public DqtApiClient(HttpClient httpClient)
    {
        _client = httpClient;
    }

    public async Task<TeacherInfo?> GetTeacherByTrn(string trn)
    {
        var response = await _client.GetAsync($"/v2/teachers/{trn}");

        if ((int)response.StatusCode == StatusCodes.Status404NotFound)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TeacherInfo>();
    }

    public async Task SetTeacherIdentityInfo(DqtTeacherIdentityInfo info)
    {
        var response = await _client.PutAsJsonAsync($"/v2/teachers/teacher-identity/{info.Trn}", new { TsPersonId = info.UserId });
        response.EnsureSuccessStatusCode();
    }
}
