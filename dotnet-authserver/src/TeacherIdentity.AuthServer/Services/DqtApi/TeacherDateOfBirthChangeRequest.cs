namespace TeacherIdentity.AuthServer.Services.DqtApi;

public record TeacherDateOfBirthChangeRequest
{
    public required DateOnly DateOfBirth { get; init; }
    public required string EvidenceFileName { get; init; }
    public required string EvidenceFileUrl { get; init; }
    public required string Trn { get; init; }
}
