namespace TeacherIdentity.AuthServer.Services.UserVerification;

public enum EmailPinGenerationFailedReasons
{
    None,
    RateLimitExceeded,
    InvalidAddress,
    NonPersonalAddress,
}

public static class PinGenerationFailedReasonsExtensions
{
    public static EmailPinGenerationFailedReasons ToEmailPinGenerationFailedReasons(this PinGenerationFailedReasons reason)
    {
        switch (reason)
        {
            case PinGenerationFailedReasons.None:
                return EmailPinGenerationFailedReasons.None;
            case PinGenerationFailedReasons.RateLimitExceeded:
                return EmailPinGenerationFailedReasons.RateLimitExceeded;
            case PinGenerationFailedReasons.InvalidAddress:
                return EmailPinGenerationFailedReasons.InvalidAddress;
            default:
                throw new ArgumentException("Invalid pin generation failed reason");
        }
    }
}
