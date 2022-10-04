using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace TeacherIdentity.AuthServer.Api.Validation;

public static class ExceptionContextExtensions
{
    public static ObjectResult GetResultFromErrorException(this ExceptionContext exceptionContext, ErrorException ex, int statusCode)
    {
        var error = ex.Error;

        var problemDetailsFactory = exceptionContext.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

        var problemDetails = problemDetailsFactory.CreateProblemDetails(
            exceptionContext.HttpContext,
            statusCode,
            title: error.Title,
            detail: error.Detail);

        problemDetails.AddErrorCode(error);

        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }
}
