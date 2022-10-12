namespace TeacherIdentity.AuthServer.Notifications.Messages;

public class UserSignedInMessage : INotificationMessage
{
    public const string MessageTypeName = "UserSignedIn";

    public required User User { get; init; }
}
