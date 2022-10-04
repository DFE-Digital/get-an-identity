namespace TeacherIdentity.AuthServer.Models;

public class JourneyTrnLookupState
{
    public required Guid JourneyId { get; init; }
    public required DateTime Created { get; init; }
    public DateTime? Locked { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required DateOnly DateOfBirth { get; set; }
    public required string? Trn { get; set; }
    public User? User { get; set; }
    public Guid? UserId { get; set; }
    public required string? NationalInsuranceNumber { get; set; }
}
