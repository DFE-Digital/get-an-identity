namespace TeacherIdentity.AuthServer.Notifications.Messages;

public class UserSignedInMessage : INotificationMessage
{
    public required User User { get; init; }
}
