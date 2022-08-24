using Hangfire;

namespace TeacherIdentity.AuthServer.Services.Email;

public class BackgroundEmailSender : IEmailSender
{
    private readonly IEmailSender _innerEmailSender;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public BackgroundEmailSender(IBackgroundJobClient backgroundJobClient, IEmailSender innerEmailSender)
    {
        _innerEmailSender = innerEmailSender;
        _backgroundJobClient = backgroundJobClient;
    }

    public Task SendEmail(string to, string subject, string body)
    {
        _backgroundJobClient.Enqueue<BackgroundEmailSender>(
            sender => sender.SendEmailInner(to, subject, body));

        return Task.CompletedTask;
    }

    public Task SendEmailInner(string to, string subject, string body) =>
        _innerEmailSender.SendEmail(to, subject, body);
}
