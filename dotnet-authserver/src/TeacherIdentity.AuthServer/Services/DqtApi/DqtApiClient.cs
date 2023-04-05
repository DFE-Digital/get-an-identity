using System.Text;
using System.Text.Json;
using Flurl;

namespace TeacherIdentity.AuthServer.Services.DqtApi;

public class DqtApiClient : IDqtApiClient
{
    private readonly HttpClient _client;

    public DqtApiClient(HttpClient httpClient)
    {
        _client = httpClient;
    }

    public async Task<FindTeachersResponse> FindTeachers(FindTeachersRequest request, CancellationToken cancellationToken)
    {
        var url = new Url("/v2/teachers/find")
            .SetQueryParam("firstName", request.FirstName)
            .SetQueryParam("lastName", request.LastName)
            .SetQueryParam("previousFirstName", request.PreviousFirstName)
            .SetQueryParam("previousLastName", request.PreviousLastName)
            .SetQueryParam("dateOfBirth", request.DateOfBirth?.ToString("yyyy-MM-dd"))
            .SetQueryParam("nationalInsuranceNumber", request.NationalInsuranceNumber)
            .SetQueryParam("ittProviderName", request.IttProviderName)
            .SetQueryParam("ittProviderUkprn", request.IttProviderUkprn)
            .SetQueryParam("emailAddress", request.EmailAddress)
            .SetQueryParam("trn", request.Trn);

        var response = await _client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FindTeachersResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<TeacherInfo?> GetTeacherByTrn(string trn, CancellationToken cancellationToken)
    {

        var response = await _client.GetAsync($"/v2/teachers/{trn}", cancellationToken);

        if ((int)response.StatusCode == StatusCodes.Status404NotFound)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TeacherInfo>(cancellationToken: cancellationToken);
    }

    public async Task<GetIttProvidersResponse> GetIttProviders(CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync($"/v2/itt-providers", cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetIttProvidersResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task PostTeacherNameChange(TeacherNameChangeRequest request, CancellationToken cancellationToken = default)
    {
        HttpContent content = JsonContent.Create(request);
        var response = await _client.PostAsync("/v3/teachers/name-changes", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
