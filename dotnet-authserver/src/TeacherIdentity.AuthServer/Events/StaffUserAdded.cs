namespace TeacherIdentity.AuthServer.Events;

public class StaffUserAdded : EventBase
{
    public required Guid UserId { get; init; }
    public required string EmailAddress { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string[] StaffRoles { get; init; }
    public required Guid AddedByUserId { get; init; }
}
