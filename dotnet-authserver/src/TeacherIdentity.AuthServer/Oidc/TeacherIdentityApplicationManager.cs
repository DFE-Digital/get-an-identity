using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Oidc;

public partial class TeacherIdentityApplicationManager : OpenIddictApplicationManager<Application>
{
    public TeacherIdentityApplicationManager(
        IOpenIddictApplicationCache<Application> cache,
        ILogger<OpenIddictApplicationManager<Application>> logger,
        IOptionsMonitor<OpenIddictCoreOptions> options,
        IOpenIddictApplicationStoreResolver resolver)
        : base(cache, logger, options, resolver)
    {
    }

    public new TeacherIdentityApplicationStore Store => (TeacherIdentityApplicationStore)base.Store;

    public override IAsyncEnumerable<Application> FindByPostLogoutRedirectUriAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(address))
        {
            throw new ArgumentException("The address cannot be null or empty.", nameof(address));
        }

        var applications = Options.CurrentValue.DisableEntityCaching ?
            Store.FindByPostLogoutRedirectUriAsync(address, cancellationToken) :
            Cache.FindByPostLogoutRedirectUriAsync(address, cancellationToken);

        if (Options.CurrentValue.DisableAdditionalFiltering)
        {
            return applications;
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<Application> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var application in applications)
            {
                var addresses = await Store.GetPostLogoutRedirectUrisAsync(application, cancellationToken);

                foreach (var pattern in addresses)
                {
                    if (Application.MatchUriPattern(pattern, address, ignorePath: false))
                    {
                        yield return application;
                    }
                }
            }
        }
    }

    public override async ValueTask PopulateAsync(Application application, OpenIddictApplicationDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        await base.PopulateAsync(application, descriptor, cancellationToken);

        if (descriptor is TeacherIdentityApplicationDescriptor teacherIdentityApplicationDescriptor)
        {
            await Store.SetServiceUrlAsync(application, teacherIdentityApplicationDescriptor.ServiceUrl);
            await Store.SetTrnRequirementTypeAsync(application, teacherIdentityApplicationDescriptor.TrnRequirementType);
            await Store.SetBlockProhibitedTeachersAsync(application, teacherIdentityApplicationDescriptor.BlockProhibitedTeachers);
            await Store.SetTrnMatchPolicyAsync(application, teacherIdentityApplicationDescriptor.TrnMatchPolicy);
            await Store.SetRaiseTrnResolutionSupportTicketsAsync(application, teacherIdentityApplicationDescriptor.RaiseTrnResolutionSupportTickets);
        }
    }

    public override async ValueTask PopulateAsync(OpenIddictApplicationDescriptor descriptor, Application application, CancellationToken cancellationToken = default)
    {
        await base.PopulateAsync(descriptor, application, cancellationToken);

        if (descriptor is TeacherIdentityApplicationDescriptor teacherIdentityApplicationDescriptor)
        {
            teacherIdentityApplicationDescriptor.ServiceUrl = await Store.GetServiceUrlAsync(application);
            teacherIdentityApplicationDescriptor.TrnRequirementType = await Store.GetTrnRequirementTypeAsync(application);
            teacherIdentityApplicationDescriptor.BlockProhibitedTeachers = await Store.GetBlockProhibitedTeachersAsync(application);
            teacherIdentityApplicationDescriptor.TrnMatchPolicy = await Store.GetTrnMatchPolicyAsync(application);
            teacherIdentityApplicationDescriptor.RaiseTrnResolutionSupportTickets = await Store.GetRaiseTrnResolutionSupportTicketsAsync(application);
        }
    }

    public async ValueTask<bool> ValidateRedirectUriDomain(Application application, string address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentException.ThrowIfNullOrEmpty(address);

        foreach (var uri in await Store.GetRedirectUrisAsync(application, cancellationToken))
        {
            if (Application.MatchUriPattern(uri, address, ignorePath: true))
            {
                return true;
            }
        }

        return false;
    }

    public override async ValueTask<bool> ValidateRedirectUriAsync(Application application, string address, CancellationToken cancellationToken = default)
    {
        if (application is null)
        {
            throw new ArgumentNullException(nameof(application));
        }

        if (string.IsNullOrEmpty(address))
        {
            throw new ArgumentException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0143), nameof(address));
        }

        foreach (var uri in await Store.GetRedirectUrisAsync(application, cancellationToken))
        {
            if (Application.MatchUriPattern(uri, address, ignorePath: false))
            {
                return true;
            }
        }

        Logger.LogInformation(OpenIddictResources.GetResourceString(OpenIddictResources.ID6162), address, await GetClientIdAsync(application, cancellationToken));

        return false;
    }

    public new ValueTask<string> ObfuscateClientSecretAsync(string secret, CancellationToken cancellationToken = default) =>
        base.ObfuscateClientSecretAsync(secret, cancellationToken);
}
