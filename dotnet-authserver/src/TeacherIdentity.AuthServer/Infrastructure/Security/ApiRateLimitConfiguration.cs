using System.Security.Claims;
using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Infrastructure.Security;

public class ApiRateLimitConfiguration : RateLimitConfiguration
{
    public ApiRateLimitConfiguration(IOptions<IpRateLimitOptions> ipOptions, IOptions<ClientRateLimitOptions> clientOptions)
        : base(ipOptions, clientOptions)
    {
    }

    public override void RegisterResolvers()
    {
        base.RegisterResolvers();

        ClientResolvers.Add(new ApiClientResolveContributor());
    }

    private class ApiClientResolveContributor : IClientResolveContributor
    {
        public Task<string> ResolveClientAsync(HttpContext httpContext)
        {
            var clientId = httpContext.User.FindFirstValue(Claims.ClientId);

            if (clientId is null)
            {
                throw new Exception("Failed to identify current client");
            }

            return Task.FromResult(clientId);
        }
    }
}
