using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenIddict.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Oidc;

public class TeacherIdentityApplicationStore : OpenIddictEntityFrameworkCoreApplicationStore<Application, Authorization, Token, TeacherIdentityServerDbContext, string>
{
    public TeacherIdentityApplicationStore(
        IMemoryCache cache,
        TeacherIdentityServerDbContext context,
        IOptionsMonitor<OpenIddictEntityFrameworkCoreOptions> options)
        : base(cache, context, options)
    {
    }

    public ValueTask<string?> GetServiceUrlAsync(Application application)
    {
        if (application is null)
        {
            throw new ArgumentNullException(nameof(application));
        }

        return new ValueTask<string?>(application.ServiceUrl);
    }

    public ValueTask SetServiceUrlAsync(Application application, string? serviceUrl)
    {
        if (application is null)
        {
            throw new ArgumentNullException(nameof(application));
        }

        application.ServiceUrl = serviceUrl;

        return default;
    }
}
