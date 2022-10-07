namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public interface IEmailVerificationService
{
    Task<PinGenerationResult> GeneratePin(string email);
    Task<PinVerificationFailedReasons> VerifyPin(string email, string pin);
}
