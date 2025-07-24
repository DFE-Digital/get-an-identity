using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;
using TeacherIdentity.AuthServer.Infrastructure.Http;

namespace TeacherIdentity.AuthServer.Services.DqtApi;

public class DqtApiClient : IDqtApiClient
{
    public const int MaxEvidenceFileNameLength = 100;

    private readonly HttpClient _client;
    private readonly IOptions<OpenIddictServerOptions> _openIddictServerOptionsAccessor;
    private readonly JsonWebTokenHandler _jwtHandler = new JsonWebTokenHandler();

    private static JsonSerializerOptions _serializerOptions { get; } = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter(),
        }
    };

    public DqtApiClient(HttpClient httpClient, IOptions<OpenIddictServerOptions> openIddictServerOptionsAccessor)
    {
        _client = httpClient;
        _openIddictServerOptionsAccessor = openIddictServerOptionsAccessor;
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

    public async Task PostTeacherNameChange(string trn, TeacherNameChangeRequest body, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v3/person/name-changes")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.TryAddWithoutValidation("X-Api-Version", "20250627");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CreateJwt(trn));
        var response = await _client.SendAsync(request, cancellationToken);
        response.HandleErrorResponse();
    }

    public async Task PostTeacherDateOfBirthChange(string trn, TeacherDateOfBirthChangeRequest body, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v3/person/date-of-birth-changes")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.TryAddWithoutValidation("X-Api-Version", "20250627");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CreateJwt(trn));
        var response = await _client.SendAsync(request, cancellationToken);
        response.HandleErrorResponse();
    }

    private string CreateJwt(string trn)
    {
        var tokenDescriptor = new SecurityTokenDescriptor();
        tokenDescriptor.Issuer = _openIddictServerOptionsAccessor.Value.Issuer!.ToString();
        tokenDescriptor.SigningCredentials = _openIddictServerOptionsAccessor.Value.SigningCredentials.First();

        tokenDescriptor.Claims = new Dictionary<string, object>
        {
            ["scope"] = "dqt:read",
            ["trn"] = trn
        };

        return _jwtHandler.CreateToken(tokenDescriptor);
    }
}
