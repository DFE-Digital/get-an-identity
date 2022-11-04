using Microsoft.Extensions.Options;
using Notify.Client;

namespace TeacherIdentity.AuthServer.Services.Email;

public class NotifyEmailSender : IEmailSender
{
    private const string TemplateId = "fa26cec0-bf41-42ee-b3ac-b600f6faf8af";

    private readonly NotifyOptions _options;
    private readonly NotificationClient _notificationClient;
    private readonly NotificationClient? _noSendNotificationClient;
    private readonly ILogger<NotifyEmailSender> _logger;

    public NotifyEmailSender(IOptions<NotifyOptions> notifyOptionsAccessor, ILogger<NotifyEmailSender> logger)
    {
        _options = notifyOptionsAccessor.Value;
        _notificationClient = new NotificationClient(_options.ApiKey);
        _logger = logger;

        if (_options.ApplyDomainFiltering && !string.IsNullOrEmpty(_options.NoSendApiKey))
        {
            _noSendNotificationClient = new NotificationClient(_options.NoSendApiKey);
        }
    }

    public async Task SendEmail(string to, string subject, string body)
    {
        NotificationClient client = _notificationClient;

        if (_options.ApplyDomainFiltering)
        {
            var toDomain = to[(to.IndexOf('@') + 1)..];

            if (!_options.DomainAllowList.Contains(toDomain))
            {
                // Domain is not in allow list, use the 'no send' client instead if we have one

                if (_noSendNotificationClient is not null)
                {
                    _logger.LogDebug("Email {email} does not have domain in the allow list; using the 'no send' client.", to);
                    client = _noSendNotificationClient;
                }
                else
                {
                    _logger.LogInformation("Email {email} does not have domain in the allow list; skipping send.", to);
                    return;
                }
            }
        }

        try
        {
            await client.SendEmailAsync(
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
