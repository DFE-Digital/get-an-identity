namespace TeacherIdentity.AuthServer.Models;

public record AuthenticationStateInitializationData
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
    public TrnAssociationSource? TrnAssociationSource;
    public string? TrnToken;

    public static AuthenticationStateInitializationData FromUser(User? user)
    {
        if (user is null)
        {
            return new AuthenticationStateInitializationData();
        }

        return new AuthenticationStateInitializationData
        {
            UserId = user.UserId,
            EmailAddress = user.EmailAddress,
            EmailAddressVerified = true,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Trn = user.Trn,
            HaveCompletedTrnLookup = user.CompletedTrnLookup != null,
            UserType = user.UserType,
            StaffRoles = user.StaffRoles,
            TrnLookupStatus = user.TrnLookupStatus
        };
    }
};
