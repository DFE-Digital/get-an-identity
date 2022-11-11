namespace TeacherIdentity.AuthServer.Events;

public record WebHookUpdatedEvent : EventBase
{
    public required Guid WebHookId { get; init; }
    public required Guid UpdatedByUserId { get; init; }
    public required WebHookUpdatedEventChanges Changes { get; init; }
    public required string Endpoint { get; init; }
    public required bool Enabled { get; init; }
}

[Flags]
public enum WebHookUpdatedEventChanges
{
    None = 0,
    Endpoint = 1 << 0,
    Enabled = 1 << 1,
    Secret = 1 << 2,
}
