using Microsoft.AspNetCore.Mvc;

namespace TeacherIdentity.AuthServer.Api.Validation;

public class ErrorResult : IResult
{
    private readonly ProblemDetails _problemDetails;

    private ErrorResult(ProblemDetails problemDetails)
    {
        _problemDetails = problemDetails;
    }

    public static ErrorResult Create(Error error, int statusCode = StatusCodes.Status400BadRequest) =>
        new(new ProblemDetails()
        {
            Title = error.Title,
            Detail = error.Detail,
            Status = statusCode,
            Extensions =
            {
                { "errorCode", error.ErrorCode }
            }
        });

    public static ErrorResult Create(IDictionary<string, string[]> errors)
    {
        var error = ErrorRegistry.RequestIsNotValid();
        var statusCode = StatusCodes.Status400BadRequest;

        return new(new ValidationProblemDetails(errors)
        {
            Title = error.Title,
            Detail = error.Detail,
            Status = statusCode,
            Extensions =
            {
                { "errorCode", error.ErrorCode }
            }
        });
    }

    public Task ExecuteAsync(HttpContext httpContext) =>
        Results.Problem(_problemDetails).ExecuteAsync(httpContext);
}
