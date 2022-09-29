namespace TeacherIdentity.AuthServer.Events;

public class UserSignedIn : EventBase
{
    public Guid UserId { get; set; }
    public string? ClientId { get; set; }
    public string? Scope { get; set; }
}
