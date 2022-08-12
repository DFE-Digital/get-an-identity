namespace TeacherIdentity.AuthServer;

public interface IPinGenerator
{
    Task<string> GenerateEmailConfirmationPin(string email);
}
