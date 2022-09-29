namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public interface IWebHookNotificationSender
{
    Task SendNotification(string endpoint, string payload);
}
