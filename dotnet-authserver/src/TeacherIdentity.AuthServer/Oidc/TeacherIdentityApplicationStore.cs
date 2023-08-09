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

    public new TeacherIdentityServerDbContext Context => (TeacherIdentityServerDbContext)base.Context;

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
