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

    public async Task SendEmailAddressConfirmationEmail(string to, string code)
    {
        var subject = "Confirm your email address";
        var body = code;

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

            _logger.LogInformation("Successfully sent email address confirmation email via Notify to {Email}.", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed sending email address confirmation email via Notify to {Email}.", to);

            throw;
        }
    }
}
