namespace TeacherIdentity.AuthServer.Events;

public class StaffUserAdded : EventBase
{
    public Guid UserId { get; set; }
    public string? EmailAddress { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string[]? StaffRoles { get; set; }
    public Guid AddedByUserId { get; set; }
}
