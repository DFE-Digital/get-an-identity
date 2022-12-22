using System.Security.Claims;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Tests;

public class UserClaimHelperTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetPublicClaims_FromUser_ReturnsExpectedClaims(bool haveTrnScope)
    {
        // Arrange
        var user = new User()
        {
            UserId = Guid.NewGuid(),
            EmailAddress = Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            LastName = Faker.Name.Last(),
            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
            Trn = "1234567",
            Created = DateTime.UtcNow,
            UserType = UserType.Default,
            Updated = DateTime.UtcNow,
            TrnLookupStatus = TrnLookupStatus.Found
        };

        // Act
        var result = UserClaimHelper.GetPublicClaims(
            user,
            hasScope: scope => scope == CustomScopes.Trn && haveTrnScope);

        // Assert
        var expectedClaims = new List<Claim>()
        {
            new Claim(Claims.Subject, user.UserId.ToString()!),
            new Claim(Claims.Email, user.EmailAddress),
            new Claim(Claims.EmailVerified, bool.TrueString),
            new Claim(Claims.Name, user.FirstName + " " + user.LastName),
            new Claim(Claims.GivenName, user.FirstName),
            new Claim(Claims.FamilyName, user.LastName),
            new Claim(Claims.Birthdate, user.DateOfBirth.Value.ToString("yyyy-MM-dd")),
        };

        if (haveTrnScope)
        {
            expectedClaims.Add(new Claim(CustomClaims.Trn, user.Trn));
            expectedClaims.Add(new Claim(CustomClaims.TrnLookupStatus, user.TrnLookupStatus!.Value.ToString()));
        }

        Assert.Equal(expectedClaims.OrderBy(c => c.Type), result.OrderBy(c => c.Type), new ClaimTypeAndValueEqualityComparer());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetPublicClaims_FromAuthenticationState_ReturnsExpectedClaims(bool haveTrnScope)
    {
        // Arrange
        var user = new User()
        {
            UserId = Guid.NewGuid(),
            EmailAddress = Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            LastName = Faker.Name.Last(),
            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
            Trn = "1234567",
            CompletedTrnLookup = DateTime.UtcNow,
            Created = DateTime.UtcNow,
            UserType = UserType.Default,
            Updated = DateTime.UtcNow,
            TrnLookupStatus = TrnLookupStatus.Found
        };

        Func<string, bool> hasScope = s => s == CustomScopes.Trn && haveTrnScope;

        var userRequirements = UserRequirementsExtensions.GetUserRequirementsForScopes(hasScope);

        var authenticationState = new AuthenticationState(
            journeyId: Guid.NewGuid(),
            userRequirements,
            postSignInUrl: "",
            startedAt: DateTime.UtcNow,
            oAuthState: new OAuthAuthorizationState(
                clientId: "",
                scope: "email profile" + (haveTrnScope ? " trn" : ""),
                redirectUri: ""));

        authenticationState.OnEmailSet(user.EmailAddress);
        authenticationState.OnEmailVerified(user);

        // Act
        var result = UserClaimHelper.GetPublicClaims(authenticationState, hasScope);

        // Assert
        var expectedClaims = new List<Claim>()
        {
            new Claim(Claims.Subject, authenticationState.UserId.ToString()!),
            new Claim(Claims.Email, authenticationState.EmailAddress!),
            new Claim(Claims.EmailVerified, authenticationState.EmailAddressVerified.ToString()),
            new Claim(Claims.Name, authenticationState.FirstName + " " + authenticationState.LastName),
            new Claim(Claims.GivenName, authenticationState.FirstName!),
            new Claim(Claims.FamilyName, authenticationState.LastName!),
            new Claim(Claims.Birthdate, authenticationState.DateOfBirth!.Value.ToString("yyyy-MM-dd")),
        };

        if (haveTrnScope)
        {
            expectedClaims.Add(new Claim(CustomClaims.Trn, authenticationState.Trn!));
            expectedClaims.Add(new Claim(CustomClaims.TrnLookupStatus, authenticationState.TrnLookupStatus!.Value.ToString()));
        }

        Assert.Equal(expectedClaims.OrderBy(c => c.Type), result.OrderBy(c => c.Type), new ClaimTypeAndValueEqualityComparer());
    }
}
