namespace TeacherIdentity.AuthServer.Models;

public class User
{
    public const int FirstNameMaxLength = 200;
    public const int MiddleNameMaxLength = 200;
    public const int LastNameMaxLength = 200;
    public const int PreferredNameMaxLength = 200;

    public const string EmailAddressUniqueIndexName = "ix_users_email_address";
    public const string MobileNumberUniqueIndexName = "ix_users_mobile_number";
    public const string TrnUniqueIndexName = "ix_users_trn";

    public Guid UserId { get; set; }
    public required string EmailAddress { get; set; }
    public required string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public required string LastName { get; set; }
    public string? PreferredName { get; set; }
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
    public MobileNumber? NormalizedMobileNumber { get; set; }
    public bool TrnLookupSupportTicketCreated { get; set; }
    public TrnVerificationLevel? TrnVerificationLevel { get; set; }
    public string? NationalInsuranceNumber { get; set; }

    public TrnVerificationLevel? EffectiveVerificationLevel
    {
        get
        {
            if (Trn is null)
            {
                return null;
            }

            if (TrnVerificationLevel == Models.TrnVerificationLevel.Medium)
            {
                return Models.TrnVerificationLevel.Medium;
            }

            if (TrnAssociationSource == Models.TrnAssociationSource.TrnToken ||
                TrnAssociationSource == Models.TrnAssociationSource.SupportUi)
            {
                return Models.TrnVerificationLevel.Medium;
            }

            return Models.TrnVerificationLevel.Low;
        }
    }

    public static string NormalizeNationalInsuranceNumber(string? nationalInsuranceNumber) =>
        new string((nationalInsuranceNumber ?? string.Empty).Where(char.IsAsciiLetterOrDigit).ToArray()).ToUpper();
}
