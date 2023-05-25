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

    private static IEnumerable<NotificationEnvelope> GetNotificationsForEvent(EventBase @event)
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
                        MiddleName = userUpdated.Changes.HasFlag(UserUpdatedEventChanges.MiddleName) ? Option.Some(userUpdated.User.MiddleName) : default,
                        LastName = userUpdated.Changes.HasFlag(UserUpdatedEventChanges.LastName) ? Option.Some(userUpdated.User.LastName) : default,
                        Trn = userUpdated.Changes.HasFlag(UserUpdatedEventChanges.Trn) ? Option.Some(userUpdated.User.Trn) : default,
                        MobileNumber = userUpdated.Changes.HasFlag(UserUpdatedEventChanges.MobileNumber) ? Option.Some(userUpdated.User.MobileNumber) : default,
                        TrnLookupStatus = userUpdated.Changes.HasFlag(UserUpdatedEventChanges.TrnLookupStatus) ? Option.Some(userUpdated.User.TrnLookupStatus!.Value) : default
                    }
                },
                MessageType = UserUpdatedMessage.MessageTypeName,
                TimeUtc = userUpdated.CreatedUtc
            };
        }
        else if (@event is UserMergedEvent userMerged)
        {
            yield return new NotificationEnvelope()
            {
                NotificationId = Guid.NewGuid(),
                Message = new UserMergedMessage()
                {
                    MasterUser = userMerged.User,
                    MergedUserId = userMerged.MergedWithUserId
                },
                MessageType = UserMergedMessage.MessageTypeName,
                TimeUtc = userMerged.CreatedUtc
            };
        }
        else if (@event is UserRegisteredEvent userCreated)
        {
            if (userCreated.User.UserType == Models.UserType.Staff)
            {
                yield break;
            }

            yield return new NotificationEnvelope()
            {
                NotificationId = Guid.NewGuid(),
                Message = new UserCreatedMessage()
                {
                    User = userCreated.User
                },
                MessageType = UserCreatedMessage.MessageTypeName,
                TimeUtc = userCreated.CreatedUtc
            };
        }
    }
}
