using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;
using TeacherIdentity.AuthServer.Infrastructure.Http;

namespace TeacherIdentity.AuthServer.Services.DqtApi;

public class DqtApiClient : IDqtApiClient
{
    public const int MaxEvidenceFileNameLength = 100;

    private readonly HttpClient _client;

    private static JsonSerializerOptions _serializerOptions { get; } = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter(),
        }
    };

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
            .SetQueryParam("trn", request.Trn)
            .SetQueryParam("matchPolicy", request.TrnMatchPolicy);

        var response = await _client.GetAsync(url, cancellationToken);
        response.HandleErrorResponse();
        return (await response.Content.ReadFromJsonAsync<FindTeachersResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<TeacherInfo?> GetTeacherByTrn(string trn, CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync($"/v3/teachers/{trn}?include=PendingDetailChanges,Alerts,_AllowIdSignInWithProhibitions", cancellationToken);

        if ((int)response.StatusCode == StatusCodes.Status404NotFound)
        {
            return null;
        }

        response.HandleErrorResponse();
        return await response.Content.ReadFromJsonAsync<TeacherInfo>(options: _serializerOptions, cancellationToken: cancellationToken);
    }

    public async Task<GetIttProvidersResponse> GetIttProviders(CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync($"/v2/itt-providers", cancellationToken);
        response.HandleErrorResponse();
        return (await response.Content.ReadFromJsonAsync<GetIttProvidersResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task PostTeacherNameChange(TeacherNameChangeRequest request, CancellationToken cancellationToken = default)
    {
        HttpContent content = JsonContent.Create(request);
        var response = await _client.PostAsync("/v3/teachers/name-changes", content, cancellationToken);
        response.HandleErrorResponse();
    }

    public async Task PostTeacherDateOfBirthChange(TeacherDateOfBirthChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpContent content = JsonContent.Create(request);
        var response = await _client.PostAsync("/v3/teachers/date-of-birth-changes", content, cancellationToken);
        response.HandleErrorResponse();
    }
}
