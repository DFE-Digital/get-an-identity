namespace TeacherIdentity.AuthServer.Services;

public interface IEmailConfirmationService
{
    Task<string> GeneratePin(string email);
    Task<bool> VerifyPin(string email, string pin);
}
