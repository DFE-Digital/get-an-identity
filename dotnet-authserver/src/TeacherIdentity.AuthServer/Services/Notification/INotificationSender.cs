namespace TeacherIdentity.AuthServer.Services.Notification;

public interface INotificationSender
{
    Task SendEmail(string to, string subject, string body);

    Task SendSms(string to, string message);
}
