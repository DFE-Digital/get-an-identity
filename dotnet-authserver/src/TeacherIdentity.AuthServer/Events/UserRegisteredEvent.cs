namespace TeacherIdentity.AuthServer.Events;

public record UserRegisteredEvent : EventBase
{
    public required User User { get; init; }
    public required string? ClientId { get; init; }
}
