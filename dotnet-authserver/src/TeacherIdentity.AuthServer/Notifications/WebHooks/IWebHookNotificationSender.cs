namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public interface IWebHookNotificationSender
{
    Task SendNotification(Guid notificationId, string endpoint, string payload, string secret);
}
