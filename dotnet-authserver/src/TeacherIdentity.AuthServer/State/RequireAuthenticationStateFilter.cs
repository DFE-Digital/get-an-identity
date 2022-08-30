using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TeacherIdentity.AuthServer.State;

public class RequireAuthenticationStateFilterFactory : IFilterFactory
{
    public bool IsReusable => true;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var filter = serviceProvider.GetRequiredService<RequireAuthenticationStateFilter>();
        return filter;
    }
}

public class RequireAuthenticationStateFilter : IAuthorizationFilter
{
    private readonly ILogger<RequireAuthenticationStateFilter> _logger;

    public RequireAuthenticationStateFilter(ILogger<RequireAuthenticationStateFilter> logger)
    {
        _logger = logger;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var authenticationStateFeature = context.HttpContext.Features.Get<AuthenticationStateFeature>();

        if (authenticationStateFeature is null)
        {
            _logger.LogDebug("Request to {RequestUrl} is missing authentication state.", context.HttpContext.Request.GetEncodedUrl());
            context.Result = new BadRequestResult();
            return;
        }

        // If this request has already been completed then return an error
        // (this will typically happen when the user hits the back button)
        if (authenticationStateFeature.AuthenticationState.IsComplete())
        {
            _logger.LogDebug("Authorization journey has already been completed.");
            context.Result = new BadRequestResult();
        }
    }
}
