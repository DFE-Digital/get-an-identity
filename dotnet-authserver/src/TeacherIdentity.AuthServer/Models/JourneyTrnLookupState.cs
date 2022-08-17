namespace TeacherIdentity.AuthServer.Models;

public class JourneyTrnLookupState
{
    public Guid JourneyId { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Locked { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateOnly DateOfBirth { get; set; }
    public string? Trn { get; set; }
}
