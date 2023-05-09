namespace TeacherIdentity.AuthServer.Events;

public record ClientUpdatedEvent : EventBase
{
    public required Client Client { get; init; }
    public required Guid UpdatedByUserId { get; init; }
    public required ClientUpdatedEventChanges Changes { get; init; }
}

[Flags]
public enum ClientUpdatedEventChanges
{
    None = 0,
    ClientSecret = 1 << 0,
    DisplayName = 1 << 1,
    ServiceUrl = 1 << 2,
    RedirectUris = 1 << 3,
    PostLogoutRedirectUris = 1 << 4,
    Scopes = 1 << 5,
    GrantTypes = 1 << 6,
    TrnRequirementType = 1 << 7,
    RaiseTrnResolutionSupportTickets = 1 << 8
}
