namespace TeacherIdentity.AuthServer.Services.UserVerification;

public interface IUserVerificationService
{
    Task<PinGenerationResult> GenerateEmailPin(string email);
    Task<PinVerificationFailedReasons> VerifyEmailPin(string email, string pin);
    Task<PinGenerationResult> GenerateSmsPin(string mobileNumber);
    Task<PinVerificationFailedReasons> VerifySmsPin(string mobileNumber, string pin);
}
