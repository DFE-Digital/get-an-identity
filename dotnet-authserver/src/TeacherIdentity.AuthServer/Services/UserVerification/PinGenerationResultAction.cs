using Microsoft.AspNetCore.Mvc;

namespace TeacherIdentity.AuthServer.Services.UserVerification;

public sealed class PinGenerationResultAction
{
    public bool Success;
    public IActionResult? Result { get; private set; }

    public static PinGenerationResultAction Failed(IActionResult result)
    {
        return new()
        {
            Success = false,
            Result = result,
        };
    }

    public static PinGenerationResultAction Succeeded()
    {
        return new()
        {
            Success = true,
        };
    }
}
