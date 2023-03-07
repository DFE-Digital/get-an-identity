namespace TeacherIdentity.AuthServer.Services.UserVerification;

public enum PinGenerationFailedReason
{
    None,
    RateLimitExceeded,
    InvalidAddress,
}
