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

public static class PinVerificationFailedReasonsExtensions
{
    public static bool ShouldGenerateAnotherCode(this PinVerificationFailedReasons reasons) =>
        reasons.HasFlag(PinVerificationFailedReasons.ExpiredLessThanTwoHoursAgo);
}
