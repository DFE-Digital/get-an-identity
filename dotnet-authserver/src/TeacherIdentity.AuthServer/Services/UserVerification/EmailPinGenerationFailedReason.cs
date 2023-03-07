namespace TeacherIdentity.AuthServer.Services.UserVerification;

public enum EmailPinGenerationFailedReason
{
    None,
    RateLimitExceeded,
    InvalidAddress,
    NonPersonalAddress,
}

public static class PinGenerationFailedReasonExtensions
{
    public static EmailPinGenerationFailedReason ToEmailPinGenerationFailedReasons(this PinGenerationFailedReason reason)
    {
        switch (reason)
        {
            case PinGenerationFailedReason.None:
                return EmailPinGenerationFailedReason.None;
            case PinGenerationFailedReason.RateLimitExceeded:
                return EmailPinGenerationFailedReason.RateLimitExceeded;
            case PinGenerationFailedReason.InvalidAddress:
                return EmailPinGenerationFailedReason.InvalidAddress;
            default:
                throw new ArgumentException("Invalid pin generation failed reason");
        }
    }
}
