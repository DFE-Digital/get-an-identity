namespace TeacherIdentity.AuthServer.Events;

public class ClientAddedEvent : EventBase
{
    public required Client Client { get; init; }
    public required Guid AddedByUserId { get; init; }
}
