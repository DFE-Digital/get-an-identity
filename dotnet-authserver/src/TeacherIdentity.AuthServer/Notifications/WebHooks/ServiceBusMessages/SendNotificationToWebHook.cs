namespace TeacherIdentity.AuthServer.Notifications.WebHooks.ServiceBusMessages;

public class SendNotificationToWebHook
{
    public required string Endpoint { get; init; }
    public required string Payload { get; init; }
}
