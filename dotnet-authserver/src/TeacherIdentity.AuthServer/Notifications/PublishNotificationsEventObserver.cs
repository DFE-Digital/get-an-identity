using Optional;
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

    public Task OnEventSaved(EventBase @event)
    {
        // Background generating and sending notifications
        Task.Run(async () =>
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
        });

        return Task.CompletedTask;
    }

    private IEnumerable<NotificationEnvelope> GetNotificationsForEvent(EventBase @event)
    {
        if (@event is UserUpdatedEvent userUpdated)
        {
            if (userUpdated.User.UserType == Models.UserType.Staff)
            {
                yield break;
            }

            yield return new NotificationEnvelope()
            {
                NotificationId = Guid.NewGuid(),
                Message = new UserUpdatedMessage()
                {
                    User = userUpdated.User,
                    Changes = new()
                    {
                        DateOfBirth = userUpdated.Changes.HasFlag(UserUpdatedEventChanges.DateOfBirth) ? Option.Some(userUpdated.User.DateOfBirth) : default,
                        EmailAddress = userUpdated.Changes.HasFlag(UserUpdatedEventChanges.EmailAddress) ? Option.Some(userUpdated.User.EmailAddress) : default,
                        FirstName = userUpdated.Changes.HasFlag(UserUpdatedEventChanges.FirstName) ? Option.Some(userUpdated.User.FirstName) : default,
                        LastName = userUpdated.Changes.HasFlag(UserUpdatedEventChanges.LastName) ? Option.Some(userUpdated.User.LastName) : default,
                        Trn = userUpdated.Changes.HasFlag(UserUpdatedEventChanges.Trn) ? Option.Some(userUpdated.User.Trn) : default
                    }
                },
                MessageType = UserUpdatedMessage.MessageTypeName,
                TimeUtc = userUpdated.CreatedUtc
            };
        }
    }
}
