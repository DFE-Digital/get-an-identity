namespace TeacherIdentity.AuthServer.Services.Email;

public class NoopEmailSender : IEmailSender
{
    public Task SendEmailAddressConfirmationEmail(string to, string code) => Task.CompletedTask;
}
