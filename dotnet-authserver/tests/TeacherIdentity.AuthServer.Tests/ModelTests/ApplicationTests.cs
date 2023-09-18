using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.ModelTests;

public class ApplicationTests
{
    [Theory]
    // Exact match
    [InlineData("https://localhost:3000/callback", "https://localhost:3000/callback", false, true)]
    [InlineData("https://localhost:3000/callback", "https://localhost:3000/callback", true, true)]
    // Scheme mismatch
    [InlineData("https://localhost:3000/callback", "http://localhost:3000/callback", false, false)]
    [InlineData("https://localhost:3000/callback", "http://localhost:3000/callback", true, false)]
    // Path mismatch
    [InlineData("https://localhost:3000/", "https://localhost:3000/callback", false, false)]
    [InlineData("https://localhost:3000/", "https://localhost:3000/callback", true, true)]
    // Wildcard domain
    [InlineData("https://__.london.cloudapps.digital/callback", "https://reviewapp123.london.cloudapps.digital/callback", false, true)]
    [InlineData("https://__.london.cloudapps.digital/callback", "https://reviewapp123.london.cloudapps.digital/callback", true, true)]
    [InlineData("https://__.london.cloudapps.digital/", "https://reviewapp123.london.cloudapps.digital/callback", false, false)]
    [InlineData("https://__.london.cloudapps.digital/", "https://reviewapp123.london.cloudapps.digital/callback", true, true)]
    public void MatchUriPattern_ReturnsExpectedResult(string pattern, string uri, bool ignorePath, bool expectedResult)
    {
        // Arrange

        // Act
        var result = Application.MatchUriPattern(pattern, uri, ignorePath);

        // Assert
        Assert.Equal(expectedResult, result);
    }
}
