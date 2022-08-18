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

    public Task SendEmailAddressConfirmationEmail(string to, string code)
    {
        _backgroundJobClient.Enqueue<BackgroundEmailSender>(
            sender => sender.SendEmailAddressConfirmationEmailInner(to, code));

        return Task.CompletedTask;
    }

    public Task SendEmailAddressConfirmationEmailInner(string to, string code) =>
        _innerEmailSender.SendEmailAddressConfirmationEmail(to, code);
}
