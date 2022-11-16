namespace TeacherIdentity.AuthServer.Events;

public record StaffUserUpdatedEvent : EventBase
{
    public required User User { get; init; }
    public required Guid UpdatedByUserId { get; init; }
    public required StaffUserUpdatedChanges Changes { get; init; }
}

[Flags]
public enum StaffUserUpdatedChanges
{
    None = 0,
    Email = 1 << 0,
    FirstName = 1 << 1,
    LastName = 1 << 2,
    StaffRoles = 1 << 3,
}
