using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TeacherIdentity.AuthServer.Api.Validation;

public class CamelCaseErrorKeysProblemDetailsFactory : ProblemDetailsFactory
{
    private readonly ProblemDetailsFactory _innerFactory;

    public CamelCaseErrorKeysProblemDetailsFactory(ProblemDetailsFactory innerFactory)
    {
        _innerFactory = innerFactory;
    }

    public override ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        return _innerFactory.CreateProblemDetails(httpContext, statusCode, title, type, detail, instance);
    }

    public override ValidationProblemDetails CreateValidationProblemDetails(
        HttpContext httpContext,
        ModelStateDictionary modelStateDictionary,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        var error = ErrorRegistry.RequestIsNotValid();

        var problemDetails = _innerFactory.CreateValidationProblemDetails(
            httpContext,
            modelStateDictionary,
            statusCode,
            title: error.Title);

        problemDetails.AddErrorCode(error);

        // All our property names are camel cased; ensure error keys are camel cased too

        foreach (var errorKey in problemDetails.Errors.Keys.ToArray())
        {
            var errors = problemDetails.Errors[errorKey];
            problemDetails.Errors.Remove(errorKey);

            var camelCasedKey = CamelCaseKey(errorKey);
            problemDetails.Errors.Add(camelCasedKey, errors);
        }

        return problemDetails;

        static string CamelCaseKey(string key) =>
            string.Join(".", key.Split('.').Select(System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName));
    }
}
