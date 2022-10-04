using Microsoft.AspNetCore.Mvc;

namespace TeacherIdentity.AuthServer.Api.Validation;

public static class ProblemDetailsExceptions
{
    public static void AddErrorCode(this ProblemDetails problemDetails, Error error)
    {
        problemDetails.Extensions.Add("errorCode", error.ErrorCode);
    }
}
