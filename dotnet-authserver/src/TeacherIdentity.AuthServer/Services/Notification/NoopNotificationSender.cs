namespace TeacherIdentity.AuthServer.Services.Notification;

public class NoopNotificationSender : INotificationSender
{
    public Task SendEmail(string to, string subject, string body) => Task.CompletedTask;
    public Task SendSms(string to, string message) => Task.CompletedTask;
}
