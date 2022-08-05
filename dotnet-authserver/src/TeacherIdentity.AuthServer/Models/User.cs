namespace TeacherIdentity.AuthServer.Models;

public class User
{
    public Guid UserId { get; set; }
    public string? EmailAddress { get; set; }
    public string? Trn { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
