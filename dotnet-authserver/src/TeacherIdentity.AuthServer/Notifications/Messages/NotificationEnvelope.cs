namespace TeacherIdentity.AuthServer.Notifications.Messages;

public class NotificationEnvelope
{
    public required DateTime TimeUtc { get; init; }
    public required string MessageType { get; init; }
    public required INotificationMessage Message { get; init; }
}
