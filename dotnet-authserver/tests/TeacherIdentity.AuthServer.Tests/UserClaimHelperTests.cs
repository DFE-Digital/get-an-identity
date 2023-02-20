using System.Security.Claims;
using TeacherIdentity.AuthServer.Models;
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetPublicClaims_FromUserWithMergedUsers_ReturnsPreviousUserIdClaim(bool hasMergedUsers)
    {
        // Arrange
        User? mergedUser = null;
        User? anotherMergedUser = null;

        var user = await _dbFixture.TestData.CreateUser();
        if (hasMergedUsers)
        {
            mergedUser = await _dbFixture.TestData.CreateUser(mergedWithUserId: user.UserId);
            anotherMergedUser = await _dbFixture.TestData.CreateUser(mergedWithUserId: user.UserId);
        }

        using var dbContext = _dbFixture.GetDbContext();
        var userClaimHelper = new UserClaimHelper(dbContext);

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        var result = await userClaimHelper.GetPublicClaims(
            user.UserId,
            hasScope: scope => false);
#pragma warning restore CS0618 // Type or member is obsolete

        // Assert
        if (hasMergedUsers)
        {
            var expectedPreviousUserIdClaims = new List<Claim>()
            {
                new Claim(CustomClaims.PreviousUserId, mergedUser!.UserId.ToString()),
                new Claim(CustomClaims.PreviousUserId, anotherMergedUser!.UserId.ToString()),
            };
            Assert.Equal(
                expectedPreviousUserIdClaims.OrderBy(c => c.Value),
                result.Where(c => c.Type == CustomClaims.PreviousUserId).OrderBy(c => c.Value),
                new ClaimTypeAndValueEqualityComparer());
        }
        else
        {
            Assert.DoesNotContain(result, c => c.Type == CustomClaims.PreviousUserId);
        }
    }
}
