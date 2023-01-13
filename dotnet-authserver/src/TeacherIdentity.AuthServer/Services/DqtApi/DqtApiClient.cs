namespace TeacherIdentity.AuthServer.Services.DqtApi;

public class DqtApiClient : IDqtApiClient
{
    private readonly HttpClient _client;

    public DqtApiClient(HttpClient httpClient)
    {
        _client = httpClient;
    }

    public async Task<TeacherInfo?> GetTeacherByTrn(string trn, CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync($"/v2/teachers/{trn}", cancellationToken);

        if ((int)response.StatusCode == StatusCodes.Status404NotFound)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TeacherInfo>();
    }
}
