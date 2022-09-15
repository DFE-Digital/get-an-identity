namespace TeacherIdentity.AuthServer.Api.V1.ApiModels;

public class TeacherInfo
{
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Trn { get; set; }
}
