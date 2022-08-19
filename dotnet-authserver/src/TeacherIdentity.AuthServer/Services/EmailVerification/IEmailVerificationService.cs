namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public interface IEmailVerificationService
{
    Task<string> GeneratePin(string email);
    Task<PinVerificationFailedReasons> VerifyPin(string email, string pin);
}
