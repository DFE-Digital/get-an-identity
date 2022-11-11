namespace TeacherIdentity.AuthServer.Events;

public record StaffUserAddedEvent : EventBase
{
    public required User User { get; init; }
    public required Guid AddedByUserId { get; init; }
}
