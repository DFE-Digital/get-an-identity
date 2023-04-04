using Microsoft.AspNetCore.DataProtection;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Infrastructure.Middleware;

public class ClientRedirectInfoMiddleware
{
    private readonly IDataProtector _dataProtector;
    private readonly ILogger<ClientRedirectInfoMiddleware> _logger;
    private readonly RequestDelegate _next;

    public ClientRedirectInfoMiddleware(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<ClientRedirectInfoMiddleware> logger,
        RequestDelegate next)
    {
        _dataProtector = dataProtectionProvider.CreateProtector(nameof(ClientRedirectInfo));
        _logger = logger;
        _next = next;
    }

    public async Task Invoke(HttpContext context, TeacherIdentityApplicationManager applicationManager)
    {
        ClientRedirectInfo? clientRedirectInfo = null;

        // Look for an existing encoded ClientRedirectInfo query parameter
        if (context.Request.Query.TryGetValue(ClientRedirectInfo.QueryParameterName, out var encodedQueryParam))
        {
            if (!ClientRedirectInfo.TryDecode(encodedQueryParam!, _dataProtector, out clientRedirectInfo))
            {
                _logger.LogDebug("Failed decoding encoded query parameter: '{QueryParameter}'.", encodedQueryParam!);
                OnInvalidRequest();
                return;
            }
        }
        // No encoded query parameter found, check for unencoded client_id, redirect_uri and signout_uri
        else if (context.Request.Query.TryGetValue("client_id", out var clientId) &&
            context.Request.Query.TryGetValue("redirect_uri", out var redirectUri) &&
            context.Request.Query.TryGetValue("sign_out_uri", out var signOutUri))
        {
            var client = await applicationManager.FindByClientIdAsync(clientId!);

            if (client is null)
            {
                _logger.LogDebug("Unknown client ID: '{ClientId}'.", clientId!);
                OnInvalidRequest();
                return;
            }

            if (!await applicationManager.ValidateRedirectUriDomain(client, redirectUri!))
            {
                _logger.LogDebug("Invalid redirect URI '{RedirectUri}' specified for client: '{ClientId}'.", redirectUri!, clientId!);
                OnInvalidRequest();
                return;
            }

            if (!await applicationManager.ValidateRedirectUriDomain(client, signOutUri!))
            {
                _logger.LogDebug("Invalid sign out URI '{SignOutUri}' specified for client: '{ClientId}'.", signOutUri!, clientId!);
                OnInvalidRequest();
                return;
            }

            clientRedirectInfo = new ClientRedirectInfo(_dataProtector, clientId!, redirectUri!, signOutUri!);
        }

        if (clientRedirectInfo is not null)
        {
            context.Features.Set(new ClientRedirectInfoFeature(clientRedirectInfo));
        }

        await _next(context);

        void OnInvalidRequest() => context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
}

public class ClientRedirectInfoFeature
{
    public ClientRedirectInfoFeature(ClientRedirectInfo clientRedirectInfo)
    {
        ClientRedirectInfo = clientRedirectInfo;
    }

    public ClientRedirectInfo ClientRedirectInfo { get; }
}
