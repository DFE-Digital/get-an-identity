namespace TeacherIdentity.AuthServer.Models;

public class TrnTokenModel
{
    public const int EmailAddressMaxLength = 200;
    public const string EmailAddressUniqueIndexName = "ix_trn_tokens_email_address";

    public required string TrnToken { get; set; }
    public required string Trn { get; set; }
    public required string Email { get; set; }
    public required DateTime CreatedUtc { get; set; }
    public required DateTime ExpiresUtc { get; set; }
}
