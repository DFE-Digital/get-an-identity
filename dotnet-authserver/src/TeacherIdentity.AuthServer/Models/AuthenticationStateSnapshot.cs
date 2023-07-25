namespace TeacherIdentity.AuthServer.Models;

public class AuthenticationStateSnapshot
{
    public required Guid SnapshotId { get; init; }
    public required Guid JourneyId { get; init; }
    public required string Payload { get; set; }
    public required DateTime Created { get; init; }
}
