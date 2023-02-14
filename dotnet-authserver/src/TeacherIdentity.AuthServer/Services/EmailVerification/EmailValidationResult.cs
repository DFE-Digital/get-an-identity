using Microsoft.AspNetCore.Mvc;

namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public class EmailValidationResult
{
    public bool IsValid { get; private set; }
    public IActionResult? Result { get; private set; }

    public static EmailValidationResult Failed(IActionResult result)
    {
        return new()
        {
            IsValid = false,
            Result = result
        };
    }

    public static EmailValidationResult Success()
    {
        return new()
        {
            IsValid = true,
        };
    }
}
