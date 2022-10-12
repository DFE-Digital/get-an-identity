using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Services.TrnLookup;

public class FindALostTrnIntegrationHelper
{
    private readonly IOptions<FindALostTrnIntegrationOptions> _optionsAccessor;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IIdentityLinkGenerator _linkGenerator;

    public FindALostTrnIntegrationHelper(
        IOptions<FindALostTrnIntegrationOptions> optionsAccessor,
        IOpenIddictApplicationManager applicationManager,
        IHttpContextAccessor httpContextAccessor,
        IIdentityLinkGenerator linkGenerator)
    {
        _optionsAccessor = optionsAccessor;
        _applicationManager = applicationManager;
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
    }

    public FindALostTrnIntegrationOptions Options => _optionsAccessor.Value;

    public async Task<(string Url, IDictionary<string, string> FormValues)> GetHandoverRequest(AuthenticationState authenticationState)
    {
        authenticationState.EnsureOAuthState();

        var clientId = authenticationState.OAuthState.ClientId;
        var client = (Application)(await _applicationManager.FindByClientIdAsync(clientId))!;
        var clientDisplayName = client.DisplayName;
        var clientServiceUrl = authenticationState.OAuthState.ResolveServiceUrl(client);

        var request = _httpContextAccessor.HttpContext!.Request;
        var callbackUrl = $"{request.Scheme}://{request.Host}{request.PathBase}{_linkGenerator.TrnCallback()}";
        var previousUrl = $"{request.Scheme}://{request.Host}{request.PathBase}{_linkGenerator.Trn()}";

        var formValues = new Dictionary<string, string>()
        {
            { "email", authenticationState.EmailAddress! },
            { "redirect_url", callbackUrl },
            { "redirect_uri", callbackUrl },  // TEMP, for back-compat
            { "client_title", clientDisplayName ?? string.Empty },
            { "journey_id", authenticationState.JourneyId.ToString() },
            { "client_url", clientServiceUrl },
            { "previous_url", previousUrl }
        };

        var sig = CalculateSignature(formValues);
        formValues.Add("sig", sig);

        return (_optionsAccessor.Value.HandoverEndpoint, formValues);
    }

    public string CalculateSignature(IDictionary<string, string> formValues)
    {
        var sharedKeyBytes = Encoding.UTF8.GetBytes(_optionsAccessor.Value.SharedKey);

        var canonicalizedValues = string.Join(
            "&",
            formValues.OrderBy(kvp => kvp.Key).Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        var canonicalizedValuesBytes = Encoding.UTF8.GetBytes(canonicalizedValues);

        var hashBytes = HMACSHA256.HashData(sharedKeyBytes, canonicalizedValuesBytes);
        var hash = Convert.ToHexString(hashBytes);

        return hash;
    }
}
