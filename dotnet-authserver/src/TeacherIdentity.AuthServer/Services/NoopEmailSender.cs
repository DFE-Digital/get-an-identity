namespace TeacherIdentity.AuthServer.Services;

public class NoopEmailSender : IEmailSender
{
    public Task SendEmailAddressConfirmationEmail(string to, string code) => Task.CompletedTask;
}
