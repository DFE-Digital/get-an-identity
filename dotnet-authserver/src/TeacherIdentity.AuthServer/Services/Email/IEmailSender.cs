namespace TeacherIdentity.AuthServer.Services.Email;

public interface IEmailSender
{
    Task SendEmail(string to, string subject, string body);
}
