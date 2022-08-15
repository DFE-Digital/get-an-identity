namespace TeacherIdentity.AuthServer.Services;

public interface IEmailSender
{
    Task SendEmailAddressConfirmationEmail(string to, string code);
}
