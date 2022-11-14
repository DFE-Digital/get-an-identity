namespace TeacherIdentity.AuthServer.Events;

public record UserSignedOutEvent : EventBase
{
    public required User User { get; init; }
    public string? ClientId { get; init; }
}
