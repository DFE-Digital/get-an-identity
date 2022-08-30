using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace TeacherIdentity.AuthServer.Oidc;

public class ProcessAuthorizationResponseHandler : IOpenIddictServerHandler<ApplyAuthorizationResponseContext>
{
    private readonly IEnumerable<EndpointDataSource> _endpointDataSources;

    public ProcessAuthorizationResponseHandler(IEnumerable<EndpointDataSource> endpointDataSources)
    {
        _endpointDataSources = endpointDataSources;
    }

    public async ValueTask HandleAsync(ApplyAuthorizationResponseContext context)
    {
        // This handler is to replace the built-in handlers ProcessFormPostResponse, ProcessFragmentResponse and ProcessQueryResponse.
        // We do this so we can own the HTML that's generated when authorization is completed
        // (in particular to avoid the ugly built-in form generated when the FormPost response mode is used).
        //
        // We want to use the Razor Page at /Authorization/Authorize rather than generating HTML in-line.
        // We do this by fishing out the RouteEndpoint that corresponds to that Razor Page then invoking it.

        if (string.IsNullOrEmpty(context.RedirectUri))
        {
            return;
        }

        var endpoint = _endpointDataSources.SelectMany(s => s.Endpoints)
            .Where(ep => ep.Metadata.Any(m => m is PageActionDescriptor actionDescriptor && actionDescriptor.RelativePath == "/Pages/Authorization/Authorize.cshtml"))
            .Single();

        var httpContext = context.Transaction.GetHttpRequest()!.HttpContext;
        httpContext.Features.Set(context);

        await endpoint.RequestDelegate!(httpContext);

        context.HandleRequest();
    }
}
