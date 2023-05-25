namespace TeacherIdentity.AuthServer.Api.V1.ApiModels;

public record UserInfo
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string? MiddleName { get; init; }
    public required string LastName { get; init; }
    public required DateOnly? DateOfBirth { get; init; }
    public required string? Trn { get; init; }
    public required TrnLookupStatus? TrnLookupStatus { get; init; }
    public required string? MobileNumber { get; init; }
}
