namespace TeacherIdentity.AuthServer.Notifications.WebHooks.ServiceBusMessages;

public class SendNotificationToWebHook
{
    public required Guid NotificationId { get; init; }
    public required string Endpoint { get; init; }
    public required string Payload { get; init; }
    public required string? Secret { get; init; }
}
