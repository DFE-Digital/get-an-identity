namespace TeacherIdentity.AuthServer.Events;

public class StaffUserAddedEvent : EventBase
{
    public required User User { get; init; }
    public required Guid AddedByUserId { get; init; }
}
