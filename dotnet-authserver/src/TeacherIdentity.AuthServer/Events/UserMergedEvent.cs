namespace TeacherIdentity.AuthServer.Events;

public record UserMergedEvent : EventBase
{
    public required User User { get; init; }
    public required Guid MergedWithUserId { get; init; }
}
