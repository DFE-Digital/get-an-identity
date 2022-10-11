namespace TeacherIdentity.AuthServer.Services.EmailVerification;

[Flags]
public enum PinGenerationFailedReasons
{
    None = 0,
    RateLimitExceeded = 1
}
