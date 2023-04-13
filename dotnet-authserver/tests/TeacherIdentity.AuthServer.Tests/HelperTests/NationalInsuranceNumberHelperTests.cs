using TeacherIdentity.AuthServer.Helpers;

namespace TeacherIdentity.AuthServer.Tests.HelperTests;

public class NationalInsuranceNumberHelperTests
{
    [Fact]
    public void IsValid_NinoWithSuffix_ReturnsTrue()
    {
        // Arrange
        var nino = "AB123456C";

        // Act
        var isValid = NationalInsuranceNumberHelper.IsValid(nino);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_NinoWithoutSuffix_ReturnsTrue()
    {
        // Arrange
        var nino = "AB123456";

        // Act
        var isValid = NationalInsuranceNumberHelper.IsValid(nino);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_EmptyNino_ReturnsFalse()
    {
        // Arrange
        var nino = "";

        // Act
        var isValid = NationalInsuranceNumberHelper.IsValid(nino);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_ValidNinoWithSpaces_ReturnsTrue()
    {
        // Arrange
        var nino = " AB 12 3 456 C ";

        // Act
        var isValid = NationalInsuranceNumberHelper.IsValid(nino);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_ValidNinoWithMixedCaseLetters_ReturnsTrue()
    {
        // Arrange
        var nino = "Ab123456c";

        // Act
        var isValid = NationalInsuranceNumberHelper.IsValid(nino);

        // Assert
        Assert.True(isValid);
    }

    [Theory]
    [InlineData("AB123456X")]
    [InlineData("AB123456Cx")]
    [InlineData("AB12345")]
    public void IsValid_InvalidNino_ReturnsFalse(string nino)
    {
        // Arrange

        // Act
        var isValid = NationalInsuranceNumberHelper.IsValid(nino);

        // Assert
        Assert.False(isValid);
    }
}
