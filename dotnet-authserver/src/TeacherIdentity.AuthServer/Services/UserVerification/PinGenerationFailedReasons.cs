namespace TeacherIdentity.AuthServer.Services.UserVerification;

public enum PinGenerationFailedReasons
{
    None,
    RateLimitExceeded,
    InvalidAddress,
}
