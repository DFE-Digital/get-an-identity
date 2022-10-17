namespace TeacherIdentity.AuthServer.Events;

public class UserRegisteredEvent : EventBase
{
    public required User User { get; init; }
    public required string? ClientId { get; init; }
}
