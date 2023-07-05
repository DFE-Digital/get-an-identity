namespace TeacherIdentity.AuthServer.Events;

public record UserUpdatedEvent : EventBase
{
    public required UserUpdatedEventSource Source { get; init; }
    public required User User { get; init; }
    public required Guid? UpdatedByUserId { get; init; }
    public required string? UpdatedByClientId { get; init; }
    public required UserUpdatedEventChanges Changes { get; init; }
}

[Flags]
public enum UserUpdatedEventChanges
{
    None = 0,
    EmailAddress = 1 << 0,
    FirstName = 1 << 1,
    LastName = 1 << 2,
    DateOfBirth = 1 << 3,
    Trn = 1 << 4,
    TrnLookupStatus = 1 << 5,
    MobileNumber = 1 << 6,
    MiddleName = 1 << 7,
    PreferredName = 1 << 8
}

public enum UserUpdatedEventSource
{
    Api = 0,
    TrnMatchedToExistingUser = 1,
    ChangedByUser = 2,
    SupportUi = 3,
    UserImport = 4,
    TrnToken = 5,
}
