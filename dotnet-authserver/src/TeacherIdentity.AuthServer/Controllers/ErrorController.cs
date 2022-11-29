using Dfe.Analytics.AspNetCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace TeacherIdentity.AuthServer.Controllers;

[Route("error")]
public class ErrorController : Controller
{
    public IActionResult Error(int? code)
    {
        // If there is no error, return a 404
        // (prevents browsing to this page directly)
        var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var statusCodeReExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

        if (exceptionHandlerPathFeature == null && statusCodeReExecuteFeature == null)
        {
            return NotFound();
        }

        // Ensure the Analytics event we track has the original request's details
        if (statusCodeReExecuteFeature is not null)
        {
            var analyticsFeature = HttpContext.Features.Get<WebRequestEventFeature>();
            if (analyticsFeature is not null)
            {
                var @event = analyticsFeature.Event;
                @event.RequestPath = statusCodeReExecuteFeature.OriginalPath;

                @event.RequestQuery = QueryHelpers.ParseQuery(statusCodeReExecuteFeature.OriginalQueryString)
                    .ToDictionary(q => q.Key, q => q.Value.Where(v => v is not null).Select(v => v!).ToArray());
            }
        }

        var statusCode = code ?? 500;

        // Treat Forbidden as NotFound so we don't give away our internal URLs
        if (code == 403)
        {
            statusCode = 404;
        }

        var viewName = statusCode == 404 ? "NotFound" : "GenericError";
        var result = View(viewName);
        result.StatusCode = statusCode;
        return result;
    }
}
