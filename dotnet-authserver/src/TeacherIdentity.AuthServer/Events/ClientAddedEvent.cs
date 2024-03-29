namespace TeacherIdentity.AuthServer.Events;

public record ClientAddedEvent : EventBase
{
    public required Client Client { get; init; }
    public required Guid AddedByUserId { get; init; }
}
