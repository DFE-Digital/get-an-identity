using CsvHelper.Configuration.Attributes;

namespace TeacherIdentity.AuthServer.Services.UserImport;

public class UserImportRow
{
    public const string IdHeader = "ID";
    public const string EmailAddressHeader = "EMAIL_ADDRESS";
    public const string FirstNameHeader = "FIRST_NAME";
    public const string LastNameHeader = "LAST_NAME";
    public const string DateOfBirthHeader = "DATE_OF_BIRTH";

    [Name(IdHeader)]
    public string? Id { get; set; }
    [Name(EmailAddressHeader)]
    public string? EmailAddress { get; set; }
    [Name(FirstNameHeader)]
    public string? FirstName { get; set; }
    [Name(LastNameHeader)]
    public string? LastName { get; set; }
    [Name(DateOfBirthHeader)]
    public string? DateOfBirth { get; set; }
}
