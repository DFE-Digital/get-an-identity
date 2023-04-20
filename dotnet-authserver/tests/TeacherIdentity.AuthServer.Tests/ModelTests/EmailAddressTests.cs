using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.ModelTests;

public class EmailAddressTests
{
    [Theory]
    [MemberData(nameof(ValidEmailAddresses))]
    public void TryParse_ValidEmailAddress_ReturnsTrue(string emailAddress)
    {
        // Arrange

        // Act
        var valid = EmailAddress.TryParse(emailAddress, out var parsedEmailAddress);

        // Assert
        Assert.True(valid);
        Assert.NotNull(parsedEmailAddress);
    }

    [Theory]
    [MemberData(nameof(InvalidEmailAddresses))]
    public void TryParse_InvalidEmailAddress_ReturnsFalse(string emailAddress)
    {
        // Arrange

        // Act
        var valid = EmailAddress.TryParse(emailAddress, out var parsedEmailAddress);

        // Assert
        Assert.False(valid);
        Assert.Null(parsedEmailAddress);
    }

    [Theory]
    [MemberData(nameof(ValidEmailAddresses))]
    public void Parse_ValidEmailAddress_ReturnsInstance(string emailAddress)
    {
        // Arrange

        // Act
        var parsedEmailAddress = EmailAddress.Parse(emailAddress);

        // Assert
        Assert.NotNull(parsedEmailAddress);
    }

    [Theory]
    [MemberData(nameof(InvalidEmailAddresses))]
    public void Parse_InvalidEmailAddress_ThrowsFormatException(string emailAddress)
    {
        // Arrange

        // Act
        var ex = Record.Exception(() => EmailAddress.Parse(emailAddress));

        // Assert
        Assert.IsType<FormatException>(ex);
    }

    public static IEnumerable<object[]> ValidEmailAddresses => _validEmailAddresses.Select(n => new object[] { n });

    public static IEnumerable<object[]> InvalidEmailAddresses => _invalidEmailAddresses.Select(n => new object[] { n });

    private static readonly string[] _validEmailAddresses = new[]
    {
        "email@domain.com",
        "email@domain.COM",
        "firstname.lastname@domain.com",
        "firstname.o'lastname@domain.com",
        "email@subdomain.domain.com",
        "firstname+lastname@domain.com",
        "1234567890@domain.com",
        "email@domain-one.com",
        "_______@domain.com",
        "email@domain.name",
        "email@domain.superlongtld",
        "email@domain.co.jp",
        "firstname-lastname@domain.com",
        "info@german-financial-services.vermögensberatung",
        "info@german-financial-services.reallylongarbitrarytldthatiswaytoohugejustincase",
        "japanese-info@例え.テスト",
        "email@double--hyphen.com",
    };

    private static readonly string[] _invalidEmailAddresses = new[]
    {
        "email@123.123.123.123",
        "email@[123.123.123.123]",
        "plainaddress",
        "@no-local-part.com",
        "Outlook Contact <outlook-contact@domain.com>",
        "no-at.domain.com",
        "no-tld@domain",
        ";beginning-semicolon@domain.co.uk",
        "middle-semicolon@domain.co;uk",
        "trailing-semicolon@domain.com;",
        "\"email+leading-quotes@domain.com",
        "email+middle\"-quotes@domain.com",
        "\"quoted-local-part\"@domain.com",
        "\"quoted@domain.com\"",
        "lots-of-dots@domain..gov..uk",
        "two-dots..in-local@domain.com",
        "multiple@domains@domain.com",
        "spaces in local@domain.com",
        "spaces-in-domain@dom ain.com",
        "underscores-in-domain@dom_ain.com",
        "pipe-in-domain@example.com|gov.uk",
        "comma,in-local@gov.uk",
        "comma-in-domain@domain,gov.uk",
        "pound-sign-in-local£@domain.com",
        "local-with-’-apostrophe@domain.com",
        "local-with-”-quotes@domain.com",
        "domain-starts-with-a-dot@.domain.com",
        "brackets(in)local@domain.com",
        $"email-too-long-{new string('a', 320)}@example.com",
        "incorrect-punycode@xn---something.com",
    };
}
