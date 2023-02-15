namespace TeacherIdentity.AuthServer.Services.UserVerification;

[Flags]
public enum PinGenerationFailedReasons
{
    None = 0,
    RateLimitExceeded = 1,
    InvalidAddress = 2,
}
