using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Events;

public record User
{
    public required Guid UserId { get; init; }
    public required string EmailAddress { get; init; }
    public required string FirstName { get; init; }
    public required string? MiddleName { get; init; }
    public required string LastName { get; init; }
    public required string? PreferredName { get; init; }
    public required DateOnly? DateOfBirth { get; init; }
    public required UserType UserType { get; init; }
    public required string? Trn { get; init; }
    public required string? MobileNumber { get; init; }
    public required TrnAssociationSource? TrnAssociationSource { get; init; }
    public required string[] StaffRoles { get; init; } = Array.Empty<string>();
    public required TrnLookupStatus? TrnLookupStatus { get; init; }
    public required TrnVerificationLevel? TrnVerificationLevel { get; init; }
    public required string? NationalInsuranceNumber { get; init; }

    public static User FromModel(Models.User user) => new()
    {
        DateOfBirth = user.DateOfBirth,
        EmailAddress = user.EmailAddress,
        FirstName = user.FirstName,
        MiddleName = user.MiddleName,
        LastName = user.LastName,
        PreferredName = user.PreferredName,
        StaffRoles = user.StaffRoles,
        Trn = user.Trn,
        MobileNumber = user.MobileNumber,
        NationalInsuranceNumber = user.NationalInsuranceNumber,
        TrnAssociationSource = user.TrnAssociationSource,
        TrnLookupStatus = user.TrnLookupStatus,
        TrnVerificationLevel = user.TrnVerificationLevel,
        UserId = user.UserId,
        UserType = user.UserType
    };

    public static implicit operator User(Models.User user) => FromModel(user);
}
