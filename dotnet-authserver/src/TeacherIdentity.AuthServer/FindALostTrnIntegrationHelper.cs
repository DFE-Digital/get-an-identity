using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer;

public class FindALostTrnIntegrationHelper
{
    private readonly IOptions<FindALostTrnIntegrationOptions> _optionsAccessor;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IUrlHelperFactory _urlHelperFactory;
    private readonly IActionContextAccessor _actionContextAccessor;

    public FindALostTrnIntegrationHelper(
        IOptions<FindALostTrnIntegrationOptions> optionsAccessor,
        IOpenIddictApplicationManager applicationManager,
        IUrlHelperFactory urlHelperFactory,
        IActionContextAccessor actionContextAccessor)
    {
        _optionsAccessor = optionsAccessor;
        _applicationManager = applicationManager;
        _urlHelperFactory = urlHelperFactory;
        _actionContextAccessor = actionContextAccessor;
    }

    public FindALostTrnIntegrationOptions Options => _optionsAccessor.Value;

    public async Task<(string Url, IDictionary<string, string> FormValues)> GetHandoverRequest(AuthenticationState authenticationState)
    {
        var clientId = authenticationState.GetAuthorizationRequest().ClientId!;
        var client = (await _applicationManager.FindByClientIdAsync(clientId))!;
        var clientDisplayName = await _applicationManager.GetDisplayNameAsync(client);

        var actionContext = _actionContextAccessor.ActionContext!;
        var request = actionContext.HttpContext.Request;
        var urlHelper = _urlHelperFactory.GetUrlHelper(actionContext);
        var callbackUrl = $"{request.Scheme}://{request.Host}{request.PathBase}{urlHelper.TrnCallback()}";

        var formValues = new Dictionary<string, string>()
        {
            { "email", authenticationState.EmailAddress! },
            { "redirect_uri", callbackUrl },
            { "client_title", clientDisplayName ?? string.Empty },
            { "journey_id", authenticationState.JourneyId.ToString() }
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
