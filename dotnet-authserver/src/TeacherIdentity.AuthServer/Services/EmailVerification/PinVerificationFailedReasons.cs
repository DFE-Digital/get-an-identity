namespace TeacherIdentity.AuthServer.Services.EmailVerification;

[Flags]
public enum PinVerificationFailedReasons
{
    None = 0,
    Unknown = 1,
    Expired = 2,
    ExpiredLessThanTwoHoursAgo = 4,
    NotActive = 8
}
