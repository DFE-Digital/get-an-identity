using System.Runtime.CompilerServices;
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

    public new TeacherIdentityServerDbContext Context => base.Context;

    public override IAsyncEnumerable<Application> FindByRedirectUriAsync(string address, CancellationToken cancellationToken)
    {
        // It appears that this is never actually used by the library;
        // should it ever be used the base implementation will need replacing with one that supports wildcards.
        throw new NotImplementedException();
    }

    public override async IAsyncEnumerable<Application> FindByPostLogoutRedirectUriAsync(string address, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var applications = Context.Set<Application>().AsAsyncEnumerable();

        await foreach (var application in applications.WithCancellation(cancellationToken))
        {
            var addresses = await GetPostLogoutRedirectUrisAsync(application, cancellationToken);

            foreach (var postLogoutRedirectUri in addresses)
            {
                if (Application.MatchUriPattern(postLogoutRedirectUri, address, ignorePath: false))
                {
                    yield return application;
                }
            }
        }
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

    public ValueTask<TrnRequirementType> GetTrnRequirementTypeAsync(Application application)
    {
        if (application is null)
        {
            throw new ArgumentNullException(nameof(application));
        }

        return new ValueTask<TrnRequirementType>(application.TrnRequirementType);
    }

    public ValueTask SetTrnRequirementTypeAsync(Application application, TrnRequirementType trnRequirementType)
    {
        if (application is null)
        {
            throw new ArgumentNullException(nameof(application));
        }

        application.TrnRequirementType = trnRequirementType!;

        return default;
    }

    public ValueTask<TrnMatchPolicy> GetTrnMatchPolicyAsync(Application application)
    {
        ArgumentNullException.ThrowIfNull(nameof(application));

        return new ValueTask<TrnMatchPolicy>(application.TrnMatchPolicy);
    }

    public ValueTask SetTrnMatchPolicyAsync(Application application, TrnMatchPolicy trnMatchPolicy)
    {
        ArgumentNullException.ThrowIfNull(nameof(application));

        application.TrnMatchPolicy = trnMatchPolicy;

        return default;
    }

    public ValueTask<bool> GetRaiseTrnResolutionSupportTicketsAsync(Application application)
    {
        if (application is null)
        {
            throw new ArgumentNullException(nameof(application));
        }

        return new ValueTask<bool>(application.RaiseTrnResolutionSupportTickets);
    }

    public ValueTask SetRaiseTrnResolutionSupportTicketsAsync(Application application, bool raiseTrnResolutionSupportTickets)
    {
        if (application is null)
        {
            throw new ArgumentNullException(nameof(application));
        }

        application.RaiseTrnResolutionSupportTickets = raiseTrnResolutionSupportTickets!;

        return default;
    }
}
