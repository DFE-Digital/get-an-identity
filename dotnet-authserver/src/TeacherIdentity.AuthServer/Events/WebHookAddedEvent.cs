namespace TeacherIdentity.AuthServer.Events;

public record WebHookAddedEvent : EventBase
{
    public required Guid WebHookId { get; init; }
    public required Guid AddedByUserId { get; init; }
    public required string Endpoint { get; init; }
    public required bool Enabled { get; init; }
}
