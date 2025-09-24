
namespace TeacherIdentity.AuthServer.Services.DqtApi;

public record TeacherInfo
{
    public required string Trn { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string MiddleName { get; init; }
    public required DateOnly? DateOfBirth { get; init; }
    public required string? NationalInsuranceNumber { get; init; }
    public required bool PendingNameChange { get; init; }
    public required bool PendingDateOfBirthChange { get; init; }
    public required string? Email { get; init; }
    public required IReadOnlyCollection<AlertInfo> Alerts { get; init; }
    public required bool AllowIdSignInWithProhibitions { get; set; }
}

public record AlertInfo
{
    public required AlertType AlertType { get; init; }
    public required string DqtSanctionCode { get; init; }
    public required DateOnly? EndDate { get; init; }
}

public enum AlertType
{
    Prohibition
}

