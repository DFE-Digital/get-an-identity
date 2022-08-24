namespace TeacherIdentity.AuthServer.Services.Email;

public class NoopEmailSender : IEmailSender
{
    public Task SendEmail(string to, string subject, string body) => Task.CompletedTask;
}
