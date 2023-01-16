namespace TeacherIdentity.AuthServer.Services.DqtApi;

public record TeacherInfo
{
    public required string Trn { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string? NationalInsuranceNumber { get; init; }
}
