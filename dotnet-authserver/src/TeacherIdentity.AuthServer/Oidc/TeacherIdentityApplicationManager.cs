using System.Text.RegularExpressions;
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

    public override async ValueTask PopulateAsync(Application application, OpenIddictApplicationDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        await base.PopulateAsync(application, descriptor, cancellationToken);

        await Store.SetServiceUrlAsync(application, (descriptor as TeacherIdentityApplicationDescriptor)?.ServiceUrl);
    }

    public override async ValueTask PopulateAsync(OpenIddictApplicationDescriptor descriptor, Application application, CancellationToken cancellationToken = default)
    {
        await base.PopulateAsync(descriptor, application, cancellationToken);

        if (descriptor is TeacherIdentityApplicationDescriptor teacherIdentityApplicationDescriptor)
        {
            teacherIdentityApplicationDescriptor.ServiceUrl = await Store.GetServiceUrlAsync(application);
        }
    }

    public override async ValueTask<bool> ValidateRedirectUriAsync(Application application, string address, CancellationToken cancellationToken = default)
    {
        // This is a modified form of the standard implementation with support for a __ wildcard in a redirect URI

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
            if (WildcardPathSegmentPattern().IsMatch(uri))
            {
                var pattern = $"^{Regex.Escape(uri).Replace("__", ".*")}$";

                if (Regex.IsMatch(address, pattern))
                {
                    return true;
                }
                else
                {
                    continue;
                }
            }

            // Note: the redirect_uri must be compared using case-sensitive "Simple String Comparison".
            // See http://openid.net/specs/openid-connect-core-1_0.html#AuthRequest for more information.
            if (string.Equals(uri, address, StringComparison.Ordinal))
            {
                return true;
            }
        }

        Logger.LogInformation(OpenIddictResources.GetResourceString(OpenIddictResources.ID6162), address, await GetClientIdAsync(application, cancellationToken));

        return false;
    }

    [GeneratedRegex("\\/__\\.")]
    private static partial Regex WildcardPathSegmentPattern();
}
