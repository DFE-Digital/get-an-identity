using Optional;

namespace TeacherIdentity.AuthServer.Notifications.Messages;

public record UserUpdatedMessage : INotificationMessage
{
    public const string MessageTypeName = "UserUpdated";

    public required User User { get; init; }
    public required UserUpdatedMessageChanges Changes { get; init; }
}

public record UserUpdatedMessageChanges
{
    public required Option<string> EmailAddress { get; init; }
    public required Option<string> FirstName { get; init; }
    public required Option<string> LastName { get; init; }
    public required Option<DateOnly?> DateOfBirth { get; init; }
    public required Option<string?> Trn { get; init; }
    public required Option<string?> MobileNumber { get; init; }
    public required Option<TrnLookupStatus> TrnLookupStatus { get; init; }
}
