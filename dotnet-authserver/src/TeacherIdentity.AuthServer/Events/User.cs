using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Events;

public class User
{
    public required Guid UserId { get; init; }
    public required string EmailAddress { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateOnly? DateOfBirth { get; init; }
    public required UserType UserType { get; init; }
    public required string? Trn { get; init; }
    public required string[] StaffRoles { get; init; } = Array.Empty<string>();

    public static User FromModel(Models.User user) => new()
    {
        DateOfBirth = user.DateOfBirth,
        EmailAddress = user.EmailAddress,
        FirstName = user.FirstName,
        LastName = user.LastName,
        StaffRoles = user.StaffRoles,
        Trn = user.Trn,
        UserId = user.UserId,
        UserType = user.UserType
    };
}