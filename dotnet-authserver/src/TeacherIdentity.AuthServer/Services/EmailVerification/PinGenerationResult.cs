namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public sealed class PinGenerationResult
{
    public string? Pin { get; private set; }
    public PinGenerationFailedReasons FailedReasons { get; private set; }
    public bool Succeeded => FailedReasons == PinGenerationFailedReasons.None;

    public static PinGenerationResult Failed(PinGenerationFailedReasons failedReasons)
    {
        return new()
        {
            FailedReasons = failedReasons
        };
    }

    public static PinGenerationResult Success(string pin)
    {
        return new()
        {
            Pin = pin,
            FailedReasons = PinGenerationFailedReasons.None
        };
    }
}
