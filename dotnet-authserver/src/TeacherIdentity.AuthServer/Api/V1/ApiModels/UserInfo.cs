namespace TeacherIdentity.AuthServer.Api.V1.ApiModels;

public class UserInfo
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateOnly? DateOfBirth { get; init; }
    public required string? Trn { get; init; }
}
