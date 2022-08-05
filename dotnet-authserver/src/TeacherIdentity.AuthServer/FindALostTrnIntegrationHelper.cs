using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Flurl;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer;

public class FindALostTrnIntegrationHelper
{
    private readonly IOptions<FindALostTrnIntegrationOptions> _optionsAccessor;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IUrlHelperFactory _urlHelperFactory;
    private readonly IActionContextAccessor _actionContextAccessor;
    private readonly ILogger<FindALostTrnIntegrationHelper> _logger;

    public FindALostTrnIntegrationHelper(
        IOptions<FindALostTrnIntegrationOptions> optionsAccessor,
        IOpenIddictApplicationManager applicationManager,
        IUrlHelperFactory urlHelperFactory,
        IActionContextAccessor actionContextAccessor,
        ILogger<FindALostTrnIntegrationHelper> logger)
    {
        _optionsAccessor = optionsAccessor;
        _applicationManager = applicationManager;
        _urlHelperFactory = urlHelperFactory;
        _actionContextAccessor = actionContextAccessor;
        _logger = logger;
    }

    public async Task<string> GetHandoverUrl(AuthenticationState authenticationState)
    {
        var clientId = authenticationState.GetAuthorizationRequest().ClientId!;
        var client = (await _applicationManager.FindByClientIdAsync(clientId))!;
        var clientDisplayName = await _applicationManager.GetDisplayNameAsync(client);

        var actionContext = _actionContextAccessor.ActionContext!;
        var request = actionContext.HttpContext.Request;
        var urlHelper = _urlHelperFactory.GetUrlHelper(actionContext);
        var callbackUrl = $"{request.Scheme}://{request.Host}{request.PathBase}{urlHelper.TrnCallback()}";

        var url = new Url(_optionsAccessor.Value.HandoverEndpoint)
            .SetQueryParam("email", authenticationState.EmailAddress)
            .SetQueryParam("redirect_uri", callbackUrl)
            .SetQueryParam("client_title", clientDisplayName ?? string.Empty)
            .SetQueryParam("journey_id", authenticationState.JourneyId);

        var sig = CalculateSignature(url);
        url.SetQueryParam("sig", sig);

        return url;
    }

    public bool ValidateCallback(string callbackUrl, [NotNullWhen(true)] out ClaimsPrincipal? user)
    {
        user = default;

        if (!new Url(callbackUrl).QueryParams.TryGetFirst("user", out var userJwtObj))
        {
            return false;
        }

        var userJwt = userJwtObj.ToString();

        var pskBytes = Convert.FromBase64String(_optionsAccessor.Value.SharedKey);
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(pskBytes), SecurityAlgorithms.HmacSha256Signature);

        var tokenHandler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };

        try
        {
            user = tokenHandler.ValidateToken(
                userJwt,
                new TokenValidationParameters()
                {
                    IssuerSigningKey = signingCredentials.Key,
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = false,
                    ValidateLifetime = false
                },
                out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed validating Find a lost TRN callback JWT.");
            return false;
        }

        return true;
    }

    private string CalculateSignature(string handoverUrl)
    {
        var sharedKeyBytes = Convert.FromBase64String(_optionsAccessor.Value.SharedKey);

        var queryParams = new Url(handoverUrl).Query;
        var queryParamBytes = Encoding.UTF8.GetBytes(queryParams);

        var hashBytes = HMACSHA256.HashData(sharedKeyBytes, queryParamBytes);
        var hash = Convert.ToBase64String(hashBytes);

        return hash;
    }
}
