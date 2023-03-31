namespace TeacherIdentity.AuthServer.Services.DqtApi;

public record TeacherInfo
{
    public required string Trn { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    // check this is right
    public string? MiddleName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string? NationalInsuranceNumber { get; init; }
    public bool PendingNameChange { get; init; }
    public bool PendingDateOfBirthChange { get; init; }
}
