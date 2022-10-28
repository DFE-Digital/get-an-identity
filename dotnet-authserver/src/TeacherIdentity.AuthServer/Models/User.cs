namespace TeacherIdentity.AuthServer.Models;

public class User
{
    public const int EmailAddressMaxLength = 200;
    public const int FirstNameAddressMaxLength = 200;
    public const int LastNameAddressMaxLength = 200;

    public const string EmailAddressUniqueIndexName = "ix_users_email_address";

    public Guid UserId { get; set; }
    public required string EmailAddress { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public required DateTime Created { get; set; }
    public required DateTime Updated { get; set; }
    public DateTime? LastSignedIn { get; set; }
    public DateTime? CompletedTrnLookup { get; set; }
    public UserType UserType { get; set; }
    public string? Trn { get; set; }
    public TrnAssociationSource? TrnAssociationSource { get; set; }
    public string[] StaffRoles { get; set; } = Array.Empty<string>();
}
