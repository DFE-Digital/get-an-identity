namespace TeacherIdentity.AuthServer.Models;

public class User
{
    public const int EmailAddressMaxLength = 200;
    public const int FirstNameMaxLength = 200;
    public const int LastNameMaxLength = 200;

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
    public Application? RegisteredWithClient { get; set; }
    public string? RegisteredWithClientId { get; set; }
    public TrnLookupStatus? TrnLookupStatus { get; set; }
    public bool IsDeleted { get; set; }
    public User? MergedWithUser { get; set; }
    public virtual ICollection<User>? MergedUsers { get; set; }
    public Guid? MergedWithUserId { get; set; }
    public string? MobileNumber { get; set; }
}
