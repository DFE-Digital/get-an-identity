namespace TeacherIdentity.AuthServer.Events;

public record TrnTokenAddedEvent : EventBase
{
    public required string TrnToken { get; init; }
    public required string Trn { get; set; }
    public required string Email { get; set; }
    public required DateTime ExpiresUtc { get; set; }
    public required Guid? AddedByUserId { get; init; }
    public required string? AddedByApiClientId { get; init; }
}
