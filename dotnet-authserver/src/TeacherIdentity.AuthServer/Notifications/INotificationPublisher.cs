using TeacherIdentity.AuthServer.Notifications.Messages;

namespace TeacherIdentity.AuthServer.Notifications;

public interface INotificationPublisher
{
    Task PublishNotification(NotificationEnvelope notification);
}
