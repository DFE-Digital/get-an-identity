namespace TeacherIdentity.AuthServer.Notifications.Messages;

public record UserCreatedMessage : INotificationMessage
{
    public const string MessageTypeName = "UserCreated";

    public required User User { get; init; }
}
