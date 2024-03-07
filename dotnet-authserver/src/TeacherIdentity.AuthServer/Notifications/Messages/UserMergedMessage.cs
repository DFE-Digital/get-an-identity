namespace TeacherIdentity.AuthServer.Notifications.Messages;

public record UserMergedMessage : INotificationMessage
{
    public const string MessageTypeName = "UserMerged";

    public required Guid MergedUserId { get; init; }
    public required Guid MasterUserId { get; init; }
}
