using CsvHelper.Configuration.Attributes;

namespace TeacherIdentity.AuthServer.Services.Csv;

public class UserImportRow
{
    public const string IdHeader = "ID";
    public const string EmailAddressHeader = "EMAIL_ADDRESS";
    public const string FirstNameHeader = "FIRST_NAME";
    public const string LastNameHeader = "LAST_NAME";
    public const string DateOfBirthHeader = "DATE_OF_BIRTH";

    [Name(IdHeader)]
    [Index(0)]
    public string? Id { get; set; }
    [Name(EmailAddressHeader)]
    [Index(1)]
    public string? EmailAddress { get; set; }
    [Name(FirstNameHeader)]
    [Index(2)]
    public string? FirstName { get; set; }
    [Name(LastNameHeader)]
    [Index(3)]
    public string? LastName { get; set; }
    [Name(DateOfBirthHeader)]
    [Index(4)]
    public string? DateOfBirth { get; set; }
}
