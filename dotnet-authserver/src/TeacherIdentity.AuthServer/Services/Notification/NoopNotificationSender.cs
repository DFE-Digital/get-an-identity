namespace TeacherIdentity.AuthServer.Services.Notification;

public class NoopNotificationSender : INotificationSender
{
    public Task SendEmail(string templateId, string to, IReadOnlyDictionary<string, string> personalization) => Task.CompletedTask;
    public Task SendSms(string templateId, string to, IReadOnlyDictionary<string, string> personalization) => Task.CompletedTask;
}
