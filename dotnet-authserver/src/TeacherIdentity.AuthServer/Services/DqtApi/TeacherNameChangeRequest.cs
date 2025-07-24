namespace TeacherIdentity.AuthServer.Services.DqtApi;

public record TeacherNameChangeRequest
{
    public required string EmailAddress { get; init; }
    public required string FirstName { get; init; }
    public required string? MiddleName { get; init; }
    public required string LastName { get; init; }
    public required string EvidenceFileName { get; init; }
    public required string EvidenceFileUrl { get; init; }
}
