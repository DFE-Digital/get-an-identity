using System.Security.Claims;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Tests;

public class UserClaimHelperTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _dbFixture;

    public UserClaimHelperTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetPublicClaims_FromUser_ReturnsExpectedClaims(bool haveTrnScope)
    {
        // Arrange
        var user = await _dbFixture.TestData.CreateUser(hasTrn: haveTrnScope);

        using var dbContext = _dbFixture.GetDbContext();
        var userClaimHelper = new UserClaimHelper(dbContext);

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        var result = await userClaimHelper.GetPublicClaims(
            user.UserId,
            hasScope: scope => scope == CustomScopes.Trn && haveTrnScope);
#pragma warning restore CS0618 // Type or member is obsolete

        // Assert
        var expectedClaims = new List<Claim>()
        {
            new Claim(Claims.Subject, user.UserId.ToString()!),
            new Claim(Claims.Email, user.EmailAddress),
            new Claim(Claims.EmailVerified, bool.TrueString),
            new Claim(Claims.Name, user.FirstName + " " + user.LastName),
            new Claim(Claims.GivenName, user.FirstName),
            new Claim(Claims.FamilyName, user.LastName),
            new Claim(Claims.Birthdate, user.DateOfBirth!.Value.ToString("yyyy-MM-dd")),
        };

        if (haveTrnScope)
        {
            expectedClaims.Add(new Claim(CustomClaims.Trn, user.Trn!));
            expectedClaims.Add(new Claim(CustomClaims.TrnLookupStatus, user.TrnLookupStatus!.Value.ToString()));
        }

        Assert.Equal(expectedClaims.OrderBy(c => c.Type), result.OrderBy(c => c.Type), new ClaimTypeAndValueEqualityComparer());
    }
}
