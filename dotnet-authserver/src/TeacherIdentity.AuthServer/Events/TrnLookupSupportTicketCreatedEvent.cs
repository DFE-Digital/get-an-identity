namespace TeacherIdentity.AuthServer.Events;

public record TrnLookupSupportTicketCreatedEvent : EventBase
{
    public required long TicketId { get; init; }
    public required string TicketComment { get; init; }
    public required Guid UserId { get; init; }
}
