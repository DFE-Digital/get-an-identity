using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Services.UserVerification;

public interface IUserVerificationService
{
    Task<PinGenerationResult> GenerateEmailPin(string email);
    Task<PinVerificationFailedReasons> VerifyEmailPin(string email, string pin);
    Task<PinGenerationResult> GenerateSmsPin(MobileNumber mobileNumber);
    Task<PinVerificationFailedReasons> VerifySmsPin(MobileNumber mobileNumber, string pin);
}
