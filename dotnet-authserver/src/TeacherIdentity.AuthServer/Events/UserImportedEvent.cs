namespace TeacherIdentity.AuthServer.Events;

public record UserImportedEvent : EventBase
{
    public required User User { get; init; }
    public required Guid UserImportJobId { get; init; }
}
