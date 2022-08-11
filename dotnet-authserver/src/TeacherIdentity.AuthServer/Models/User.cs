namespace TeacherIdentity.AuthServer.Models;

public class User
{
    public Guid UserId { get; set; }
    public string EmailAddress { get; set; } = null!;
    public string? Trn { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateOnly DateOfBirth { get; set; }
}
