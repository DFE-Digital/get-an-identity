namespace TeacherIdentity.AuthServer.Models;

public record AuthenticationStateInitialisationData
{
    public Guid? UserId;
    public string? EmailAddress;
    public bool EmailAddressVerified;
    public string? FirstName;
    public string? MiddleName;
    public string? LastName;
    public DateOnly? DateOfBirth;
    public string? Trn;
    public bool? HaveCompletedTrnLookup;
    public UserType? UserType;
    public string[]? StaffRoles;
    public TrnLookupStatus? TrnLookupStatus;

    public static AuthenticationStateInitialisationData FromUser(User? user)
    {
        return new AuthenticationStateInitialisationData()
        {
            UserId = user?.UserId,
            EmailAddress = user?.EmailAddress,
            EmailAddressVerified = user is not null,
            FirstName = user?.FirstName,
            LastName = user?.LastName,
            DateOfBirth = user?.DateOfBirth,
            Trn = user?.Trn,
            HaveCompletedTrnLookup = user?.CompletedTrnLookup is not null,
            UserType = user?.UserType,
            StaffRoles = user?.StaffRoles,
            TrnLookupStatus = user?.TrnLookupStatus
        };
    }
};
