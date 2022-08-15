using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Core;

namespace TeacherIdentity.AuthServer.Models;

public class TeacherIdentityApplicationManager : OpenIddictApplicationManager<Application>
{
    public TeacherIdentityApplicationManager(
        IOpenIddictApplicationCache<Application> cache,
        ILogger<OpenIddictApplicationManager<Application>> logger,
        IOptionsMonitor<OpenIddictCoreOptions> options,
        IOpenIddictApplicationStoreResolver resolver)
        : base(cache, logger, options, resolver)
    {
    }

    protected new TeacherIdentityApplicationStore Store => (TeacherIdentityApplicationStore)base.Store;

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
}
