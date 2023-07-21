namespace TeacherIdentity.AuthServer.Models;

public class AuthenticationState
{
    public required Guid JourneyId { get; init; }
    public required string Payload { get; set; }
    public required DateTime Created { get; init; }
    public required DateTime LastAccessed { get; set; }
}
