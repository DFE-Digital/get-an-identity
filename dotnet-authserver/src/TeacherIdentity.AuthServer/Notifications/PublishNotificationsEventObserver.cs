using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Notifications.Messages;

namespace TeacherIdentity.AuthServer.Notifications;

public class PublishNotificationsEventObserver : IEventObserver
{
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<PublishNotificationsEventObserver> _logger;

    public PublishNotificationsEventObserver(
        INotificationPublisher notificationPublisher,
        ILogger<PublishNotificationsEventObserver> logger)
    {
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task OnEventSaved(EventBase @event)
    {
        var notifications = GetNotificationsForEvent(@event);

        foreach (var notification in notifications)
        {
            try
            {
                await _notificationPublisher.PublishNotification(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish notification.");
            }
        }
    }

    private IEnumerable<NotificationEnvelope> GetNotificationsForEvent(EventBase @event)
    {
        if (@event is UserSignedInEvent userSignedIn)
        {
            yield return new NotificationEnvelope()
            {
                Message = new UserSignedInMessage()
                {
                    User = Messages.User.FromEvent(userSignedIn.User)
                },
                MessageType = nameof(UserSignedInMessage),
                TimeUtc = userSignedIn.CreatedUtc
            };
        }
    }
}
