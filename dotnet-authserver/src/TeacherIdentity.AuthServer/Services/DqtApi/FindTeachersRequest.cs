using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Services.DqtApi;

public record FindTeachersRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PreviousFirstName { get; init; }
    public string? PreviousLastName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? NationalInsuranceNumber { get; init; }
    public string? IttProviderName { get; init; }
    public string? IttProviderUkprn { get; init; }
    public string? EmailAddress { get; init; }
    public string? Trn { get; init; }
    public TrnMatchPolicy? TrnMatchPolicy { get; set; }
}
