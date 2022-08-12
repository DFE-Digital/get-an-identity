namespace TeacherIdentity.AuthServer.Services;

public interface IEmailConfirmationService
{
    Task<string> GeneratePin(string email);
}
