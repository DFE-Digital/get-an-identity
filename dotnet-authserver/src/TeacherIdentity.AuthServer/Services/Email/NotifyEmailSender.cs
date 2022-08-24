using Notify.Client;

namespace TeacherIdentity.AuthServer.Services.Email;

public class NotifyEmailSender : IEmailSender
{
    private const string TemplateId = "fa26cec0-bf41-42ee-b3ac-b600f6faf8af";

    private readonly NotificationClient _notificationClient;
    private readonly ILogger<NotifyEmailSender> _logger;

    public NotifyEmailSender(NotificationClient notificationClient, ILogger<NotifyEmailSender> logger)
    {
        _notificationClient = notificationClient;
        _logger = logger;
    }

    public async Task SendEmail(string to, string subject, string body)
    {
        try
        {
            await _notificationClient.SendEmailAsync(
                to,
                TemplateId,
                personalisation: new Dictionary<string, dynamic>()
                {
                    { "subject", subject },
                    { "body", body }
                });

            _logger.LogInformation("Successfully sent {Subject} email to {Email}.", subject, to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed sending {Subject} email to {Email}.", to);

            throw;
        }
    }
}
