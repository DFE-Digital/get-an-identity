namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public interface IEmailVerificationService
{
    Task<string> GeneratePin(string email);
    Task<bool> VerifyPin(string email, string pin);
}
