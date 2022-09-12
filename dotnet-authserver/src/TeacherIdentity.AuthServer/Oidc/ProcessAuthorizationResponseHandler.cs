using Microsoft.AspNetCore;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace TeacherIdentity.AuthServer.Oidc;

public class ProcessAuthorizationResponseHandler : IOpenIddictServerHandler<ApplyAuthorizationResponseContext>
{
    private readonly IIdentityLinkGenerator _linkGenerator;

    public ProcessAuthorizationResponseHandler(IIdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public ValueTask HandleAsync(ApplyAuthorizationResponseContext context)
    {
        // This handler is to replace the built-in handlers ProcessFormPostResponse, ProcessFragmentResponse and ProcessQueryResponse.
        // We do this so we can own the HTML that's generated when authorization is completed
        // (in particular to avoid the ugly built-in form generated when the FormPost response mode is used).
        //
        // Bundle the response parameters into the journey's AuthenticationState then redirect to the /sign-in/complete endpoint.

        var httpContext = context.Transaction.GetHttpRequest()!.HttpContext;
        var authenticationState = httpContext.GetAuthenticationState();

        authenticationState.AuthorizationResponseParameters = from parameter in context.Response.GetParameters()
                                                              let values = (string?[]?)parameter.Value
                                                              where values is not null
                                                              from value in values
                                                              where !string.IsNullOrEmpty(value)
                                                              select new KeyValuePair<string, string>(parameter.Key, value);

        authenticationState.AuthorizationResponseMode = context.ResponseMode;
        authenticationState.RedirectUri = context.RedirectUri;

        httpContext.Response.Redirect(_linkGenerator.CompleteAuthorization());

        context.HandleRequest();

        return default;
    }
}
