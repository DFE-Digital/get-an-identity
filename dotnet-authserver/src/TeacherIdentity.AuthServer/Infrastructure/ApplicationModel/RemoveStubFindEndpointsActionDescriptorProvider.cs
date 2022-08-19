using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Services.TrnLookup;

namespace TeacherIdentity.AuthServer.Infrastructure.ApplicationModel;

public class RemoveStubFindEndpointsActionDescriptorProvider : IActionDescriptorProvider
{
    private readonly IOptions<FindALostTrnIntegrationOptions> _optionsAccessor;

    public RemoveStubFindEndpointsActionDescriptorProvider(IOptions<FindALostTrnIntegrationOptions> optionsAccessor)
    {
        _optionsAccessor = optionsAccessor;
    }

    public int Order => int.MaxValue;

    public void OnProvidersExecuted(ActionDescriptorProviderContext context)
    {
        if (!_optionsAccessor.Value.EnableStubEndpoints)
        {
            var stubFindEndpoints = context.Results.OfType<PageActionDescriptor>()
                .Where(d => d.RelativePath.StartsWith("/Pages/StubFindALostTrn"))
                .ToArray();

            foreach (var action in stubFindEndpoints)
            {
                context.Results.Remove(action);
            }
        }
    }

    public void OnProvidersExecuting(ActionDescriptorProviderContext context)
    {
    }
}
