namespace TeacherIdentity.AuthServer.Services.Email;

public interface IEmailSender
{
    Task SendEmailAddressConfirmationEmail(string to, string code);
}
