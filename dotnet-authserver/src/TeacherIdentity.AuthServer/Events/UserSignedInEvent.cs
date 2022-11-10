namespace TeacherIdentity.AuthServer.Events;

public record UserSignedInEvent : EventBase
{
    public required User User { get; init; }
    public string? ClientId { get; init; }
    public string? Scope { get; init; }
}
