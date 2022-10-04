using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TeacherIdentity.AuthServer.Api.Validation;

namespace TeacherIdentity.AuthServer.Api.Filters;

public class HandleValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is ValidationException validationException)
        {
            foreach (var error in validationException.Errors)
            {
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            var problemDetailsFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

            var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(
                context.HttpContext,
                context.ModelState,
                statusCode: StatusCodes.Status400BadRequest)!;

            // Remove the parameter name prefix for a model-bound property as it's meaningless to consumers
            var bodyBoundParameterName = context.ActionDescriptor.Parameters
                .SingleOrDefault(p => p.BindingInfo?.BindingSource == BindingSource.Body)
                ?.Name;

            foreach (var errorKey in problemDetails.Errors.Keys.ToArray())
            {
                var errorKeyPrefix = errorKey.Split('.')[0];

                if (errorKeyPrefix == bodyBoundParameterName)
                {
                    var errorsForKey = problemDetails.Errors[errorKey];
                    problemDetails.Errors.Remove(errorKey);
                    problemDetails.Errors.Add(errorKey[(errorKeyPrefix.Length + 1)..], errorsForKey);
                }
            }

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status
            };

            context.ExceptionHandled = true;
        }
    }
}
