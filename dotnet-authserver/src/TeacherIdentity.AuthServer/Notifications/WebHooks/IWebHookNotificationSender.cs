using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications.Messages;

namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public interface IWebHookNotificationSender
{
    Task SendNotification(NotificationEnvelope notification, WebHook webHook);
}
