namespace TeacherIdentity.AuthServer.Events;

public class UserSignedIn : EventBase
{
    public required Guid UserId { get; init; }
    public string? ClientId { get; init; }
    public string? Scope { get; init; }
}
