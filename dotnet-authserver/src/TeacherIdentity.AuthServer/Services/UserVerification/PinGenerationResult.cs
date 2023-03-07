namespace TeacherIdentity.AuthServer.Services.UserVerification;

public sealed class PinGenerationResult
{
    public string? Pin { get; private set; }
    public PinGenerationFailedReason FailedReason { get; private set; }
    public bool Succeeded => FailedReason == PinGenerationFailedReason.None;

    public static PinGenerationResult Failed(PinGenerationFailedReason failedReason)
    {
        return new()
        {
            FailedReason = failedReason
        };
    }

    public static PinGenerationResult Success(string pin)
    {
        return new()
        {
            Pin = pin,
            FailedReason = PinGenerationFailedReason.None
        };
    }
}
