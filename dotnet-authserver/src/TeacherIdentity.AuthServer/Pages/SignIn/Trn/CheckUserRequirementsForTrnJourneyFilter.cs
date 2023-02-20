using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

public class CheckUserRequirementsForTrnJourneyFilterFactory : IFilterFactory
{
    public bool IsReusable => true;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<CheckUserRequirementsForTrnJourneyFilter>();
}

public class CheckUserRequirementsForTrnJourneyFilter : IPageFilter
{
    private readonly ILogger<CheckUserRequirementsForTrnJourneyFilter> _logger;

    public CheckUserRequirementsForTrnJourneyFilter(ILogger<CheckUserRequirementsForTrnJourneyFilter> logger)
    {
        _logger = logger;
    }

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.UserRequirements.HasFlag(UserRequirements.TrnHolder))
        {
            _logger.LogDebug(
                "Request to page @Page in the TRN journey was blocked as UserRequirements does not include TrnHolder.",
                context.HttpContext.Request.GetDisplayUrl());

            context.Result = new ForbidResult();
        }
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }
}
