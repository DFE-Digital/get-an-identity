using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.ModelTests;

public class MobileNumberTests
{
    [Theory]
    [MemberData(nameof(ValidPhoneNumbers))]
    public void TryParse_ValidNumber_ReturnsTrue(string number)
    {
        // Arrange

        // Act
        var valid = MobileNumber.TryParse(number, out var mobileNumber);

        // Assert
        Assert.True(valid);
        Assert.NotNull(mobileNumber);
    }

    [Theory]
    [MemberData(nameof(InvalidPhoneNumbers))]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
    public void TryParse_InvalidNumber_ReturnsFalse(string number, string _)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
    {
        // Arrange

        // Act
        var valid = MobileNumber.TryParse(number, out var mobileNumber);

        // Assert
        Assert.False(valid);
        Assert.Null(mobileNumber);
    }

    [Theory]
    [MemberData(nameof(ValidPhoneNumbers))]
    public void Parse_ValidNumber_ReturnsInstance(string number)
    {
        // Arrange

        // Act
        var mobileNumber = MobileNumber.Parse(number);

        // Assert
        Assert.NotNull(mobileNumber);
    }

    [Theory]
    [MemberData(nameof(InvalidPhoneNumbers))]
    public void Parse_InvalidNumber_ThrowsFormatException(string number, string expectedMessage)
    {
        // Arrange

        // Act
        var ex = Record.Exception(() => MobileNumber.Parse(number));

        // Assert
        Assert.IsType<FormatException>(ex);
        Assert.Equal(expectedMessage, ex.Message);
    }

    public static IEnumerable<object[]> ValidPhoneNumbers => _validInternationalPhoneNumbers.Select(n => new object[] { n });

    public static IEnumerable<object[]> InvalidPhoneNumbers => _invalidPhoneNumbers.Select(n => new object[] { n.Number, n.ExpectedErrorMessage });

    private static readonly string[] _validUkPhoneNumbers = new[]
    {
        "7123456789",
        "07123456789",
        "07123 456789",
        "07123-456-789",
        "00447123456789",
        "00 44 7123456789",
        "+447123456789",
        "+44 7123 456 789",
        "+44 (0)7123 456 789",
        "\u200B\t\t+44 (0)7123 \uFEFF 456 789 \r\n"
    };

    private static readonly string[] _validInternationalPhoneNumbers = new[]
    {
        "71234567890",  // Russia
        "1-202-555-0104",  // USA
        "+12025550104",  // USA
        "0012025550104",  // USA
        "+0012025550104",  // USA
        "23051234567",  // Mauritius,
        "+682 12345",  // Cook islands
        "+3312345678",
        "003312345678",
        "1-2345-12345-12345",  // 15 digits
    };

    private static readonly string[] _validPhoneNumbers = _validUkPhoneNumbers.Concat(_validInternationalPhoneNumbers).ToArray();

    private static readonly (string ExpectedErrorMessage, string[] Numbers)[] _invalidUkPhoneNumbers = new[]
    {
        (
            "Too many digits.",
            new[]
            {
                "712345678910",
                "0712345678910",
                "0044712345678910",
                "0044712345678910",
                "+44 (0)7123 456 789 10"
            }
        ),
        (
            "Not enough digits.",
            new[]
            {
                "0712345678",
                "004471234567",
                "00447123456",
                "+44 (0)7123 456 78"
            }
        ),
        (
            "Not a UK mobile number.",
            new[]
            {
                "08081 570364",
                "+44 8081 570364",
                "0117 496 0860",
                "+44 117 496 0860",
                "020 7946 0991",
                "+44 20 7946 0991"
            }
        ),
        (
            "Must not contain letters or symbols.",
            new[]
            {
                "07890x32109",
                "07123 456789...",
                "07123 ☟☜⬇⬆☞☝",
                "07123☟☜⬇⬆☞☝",
                "07\";DROP TABLE;\"",
                "+44 07ab cde fgh",
                "ALPHANUM3R1C"
            }
        )
    };

    private static readonly (string ExpectedErrorMessage, string Number)[] _invalidPhoneNumbers =
        _invalidUkPhoneNumbers
            .SelectMany(t => t.Numbers.Take(0).Select(n => (t.ExpectedErrorMessage, Number: n)))
            .Where(t => t.Number != "712345678910")  // Could be Russia"
            .Append(("Not a valid country prefix.", "800000000000"))
            .Append(("Not enough digits.", "1234567"))
            .Append(("Not enough digits.", "+682 1234"))  // Cook Islands phone numbers can be 5 digits
            .Append(("Too many digits.", "+12345 12345 12345 6"))
            .ToArray();
}
