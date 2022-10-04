using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Api.Validation;

namespace TeacherIdentity.AuthServer.Api.Filters;

public class DefaultErrorExceptionFilter : IExceptionFilter
{
    public DefaultErrorExceptionFilter(int statusCode)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is ErrorException ex)
        {
            context.Result = context.GetResultFromErrorException(ex, StatusCodes.Status400BadRequest);
            context.ExceptionHandled = true;
        }
    }
}
