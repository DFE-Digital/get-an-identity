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
        var errorsWithNormalizedKeys = errors.ToDictionary(kvp => CamelCaseKey(kvp.Key), kvp => kvp.Value);

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

        static string CamelCaseKey(string key) =>
            string.Join(".", key.Split('.').Select(System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName));
    }

    public Task ExecuteAsync(HttpContext httpContext) =>
        Results.Problem(_problemDetails).ExecuteAsync(httpContext);
}
