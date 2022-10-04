namespace TeacherIdentity.AuthServer.Events;

public abstract class EventBase
{
    public required DateTime CreatedUtc { get; init; }
}
