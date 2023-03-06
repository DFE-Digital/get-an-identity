using Microsoft.AspNetCore.Mvc;

namespace TeacherIdentity.AuthServer.Services.UserVerification;

public sealed class EmailPinGenerationResult
{
    public bool Success;
    public IActionResult? Result { get; private set; }

    public static EmailPinGenerationResult Failed(IActionResult result)
    {
        return new()
        {
            Success = false,
            Result = result,
        };
    }

    public static EmailPinGenerationResult Succeeded()
    {
        return new()
        {
            Success = true,
        };
    }
}
