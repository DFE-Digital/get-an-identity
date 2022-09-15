using TeacherIdentity.AuthServer.Services.BackgroundJobs;

namespace TeacherIdentity.AuthServer.Services.Email;

public class BackgroundEmailSender : IEmailSender
{
    private readonly IEmailSender _innerEmailSender;
    private readonly IBackgroundJobScheduler _backgroundJobScheduler;

    public BackgroundEmailSender(IBackgroundJobScheduler backgroundJobScheduler, IEmailSender innerEmailSender)
    {
        _innerEmailSender = innerEmailSender;
        _backgroundJobScheduler = backgroundJobScheduler;
    }

    public Task SendEmail(string to, string subject, string body) =>
        _backgroundJobScheduler.Enqueue<BackgroundEmailSender>(
            sender => sender.SendEmailInner(to, subject, body));

    public Task SendEmailInner(string to, string subject, string body) =>
        _innerEmailSender.SendEmail(to, subject, body);
}
