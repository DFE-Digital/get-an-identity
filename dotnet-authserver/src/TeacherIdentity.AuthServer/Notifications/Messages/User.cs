namespace TeacherIdentity.AuthServer.Notifications.Messages;

public record User
{
    public required Guid UserId { get; init; }
    public required string EmailAddress { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateOnly? DateOfBirth { get; init; }
    public required string? Trn { get; init; }
    public required TrnLookupStatus TrnLookupStatus { get; init; }

    public static User FromEvent(Events.User user) => new()
    {
        DateOfBirth = user.DateOfBirth,
        EmailAddress = user.EmailAddress,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Trn = user.Trn,
        TrnLookupStatus = user.TrnLookupStatus,
        UserId = user.UserId
    };

    public static implicit operator User(Events.User user) => FromEvent(user);
}
