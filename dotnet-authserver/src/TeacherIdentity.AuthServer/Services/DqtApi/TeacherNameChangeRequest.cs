namespace TeacherIdentity.AuthServer.Services.DqtApi;

public record TeacherNameChangeRequest
{
    public required string FirstName { get; init; }
    public string? MiddleName { get; init; }
    public required string LastName { get; init; }
    public required string EvidenceFileName { get; init; }
    public required string EvidenceFileUrl { get; init; }
    public required string Trn { get; init; }
}
