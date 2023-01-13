namespace TeacherIdentity.AuthServer.Services.DqtApi;

public record FindTeachersResponse
{
    public required FindTeachersResponseResult[] Results { get; init; }
}

public record FindTeachersResponseResult
{
    public required string Trn { get; init; }
    public required string[] EmailAddresses { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateOnly? DateOfBirth { get; init; }
    public required string? NationalInsuranceNumber { get; init; }
    public required string Uid { get; init; }
    public required bool HasActiveSanctions { get; init; }
}
