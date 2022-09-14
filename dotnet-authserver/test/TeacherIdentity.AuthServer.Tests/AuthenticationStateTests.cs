using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Flurl;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Tests;

public class AuthenticationStateTests
{
    [Fact]
    public void FromClaims()
    {
        // Arrange
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var email = Faker.Internet.Email();
        var emailVerified = true;
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var firstTimeUser = true;
        var haveCompletedTrnLookup = true;
        var trn = "2345678";
        var userId = Guid.NewGuid();

        var claims = new[]
        {
            new Claim(Claims.Subject, userId.ToString()!),
            new Claim(Claims.Email, email),
            new Claim(Claims.EmailVerified, emailVerified.ToString()),
            new Claim(Claims.Name, firstName + " " + lastName),
            new Claim(Claims.GivenName, firstName),
            new Claim(Claims.FamilyName, lastName),
            new Claim(Claims.Birthdate, dateOfBirth.ToString("yyyy-MM-dd")),
            new Claim(CustomClaims.HaveCompletedTrnLookup, haveCompletedTrnLookup.ToString()),
            new Claim(CustomClaims.Trn, trn)
        };

        var client = TestClients.Client1;
        var scope = "email profile trn";
        var authorizationUrl = CreateAuthorizationUrl(client, scope);

        // Act
        var authenticationState = AuthenticationState.FromClaims(authorizationUrl, client.ClientId!, scope, claims, firstTimeUser);

        // Assert
        Assert.Equal(dateOfBirth, authenticationState.DateOfBirth);
        Assert.Equal(email, authenticationState.EmailAddress);
        Assert.Equal(emailVerified, authenticationState.EmailAddressVerified);
        Assert.Equal(firstName, authenticationState.FirstName);
        Assert.Equal(lastName, authenticationState.LastName);
        Assert.Equal(firstTimeUser, authenticationState.FirstTimeUser);
        Assert.Equal(haveCompletedTrnLookup, authenticationState.HaveCompletedTrnLookup);
        Assert.Equal(trn, authenticationState.Trn);
        Assert.Equal(userId, authenticationState.UserId);
    }

    [Fact]
    public void GetClaims()
    {
        // Arrange
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var email = Faker.Internet.Email();
        var emailVerified = true;
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var firstTimeUser = true;
        var haveCompletedTrnLookup = true;
        var trn = "2345678";
        var userId = Guid.NewGuid();

        var client = TestClients.Client1;
        var scope = "email profile trn";
        var authorizationUrl = CreateAuthorizationUrl(client, scope);

        var authenticationState = new AuthenticationState(journeyId: Guid.NewGuid(), authorizationUrl, client.ClientId!, scope)
        {
            DateOfBirth = dateOfBirth,
            EmailAddress = email,
            EmailAddressVerified = emailVerified,
            FirstName = firstName,
            LastName = lastName,
            FirstTimeUser = firstTimeUser,
            HaveCompletedTrnLookup = haveCompletedTrnLookup,
            Trn = trn,
            UserId = userId
        };

        // Act
        var claims = authenticationState.GetClaims();

        // Assert
        var expectedClaims = new[]
        {
            new Claim(Claims.Subject, userId.ToString()!),
            new Claim(Claims.Email, email),
            new Claim(Claims.EmailVerified, emailVerified.ToString()),
            new Claim(Claims.Name, firstName + " " + lastName),
            new Claim(Claims.GivenName, firstName),
            new Claim(Claims.FamilyName, lastName),
            new Claim(Claims.Birthdate, dateOfBirth.ToString("yyyy-MM-dd")),
            new Claim(CustomClaims.HaveCompletedTrnLookup, haveCompletedTrnLookup.ToString()),
            new Claim(CustomClaims.Trn, trn)
        };
        Assert.Equal(expectedClaims.OrderBy(c => c.Type), claims.OrderBy(c => c.Type), new ClaimTypeAndValueEqualityComparer());
    }

    [Theory]
    [MemberData(nameof(GetNextHopUrlData))]
    public void GetNextHopUrl(AuthenticationState authenticationState, string expectedResult)
    {
        // Arrange
        var linkGenerator = A.Fake<IIdentityLinkGenerator>();

        void ConfigureMockForPage(string pageName, string returnsPath)
        {
            A.CallTo(() => linkGenerator.PageWithAuthenticationJourneyId(pageName))
                .Returns(returnsPath.SetQueryParam("asid", authenticationState.JourneyId.ToString()));
        }

        ConfigureMockForPage("/SignIn/Email", "/sign-in/email");
        ConfigureMockForPage("/SignIn/EmailConfirmation", "/sign-in/email-confirmation");
        ConfigureMockForPage("/SignIn/Trn", "/sign-in/trn");
        ConfigureMockForPage("/SignIn/TrnCallback", "/sign-in/trn-callback");

        // Act
        var result = authenticationState.GetNextHopUrl(linkGenerator);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void Populate()
    {
        // Arrange
        var created = DateTime.UtcNow;
        var user = new User()
        {
            DateOfBirth = new DateOnly(2001, 4, 1),
            EmailAddress = Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            LastName = Faker.Name.Last(),
            UserId = Guid.NewGuid(),
            Created = created,
            CompletedTrnLookup = created
        };
        var trn = "1234567";
        var firstTimeUser = true;

        var client = TestClients.Client1;
        var scope = "email profile trn";
        var authorizationUrl = CreateAuthorizationUrl(client, scope);

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, scope);

        // Act
        authenticationState.Populate(user, firstTimeUser, trn);

        // Assert
        Assert.Equal(user.DateOfBirth, authenticationState.DateOfBirth);
        Assert.Equal(user.EmailAddress, authenticationState.EmailAddress);
        Assert.Equal(user.FirstName, authenticationState.FirstName);
        Assert.Equal(user.LastName, authenticationState.LastName);
        Assert.Equal(user.UserId, authenticationState.UserId);
        Assert.True(authenticationState.EmailAddressVerified);
        Assert.Equal(firstTimeUser, authenticationState.FirstTimeUser);
        Assert.True(authenticationState.HaveCompletedTrnLookup);
        Assert.Equal(trn, authenticationState.Trn);
    }

    [Theory]
    [MemberData(nameof(GetUserTypeData))]
    public void GetUserType(string scope, UserType expectedUserType)
    {
        // Arrange
        var client = TestClients.Client1;
        var authorizationUrl = CreateAuthorizationUrl(client, scope);

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, $"email profile {scope}");

        // Act
        var userType = authenticationState.GetUserType();

        // Assert
        Assert.Equal(expectedUserType, userType);
    }

    [Theory]
    [MemberData(nameof(ValidateClaimsData))]
    public void ValidateClaims(string scope, bool expectedResult, string? expectedErrorMessage)
    {
        // Arrange
        var client = TestClients.Client1;
        var authorizationUrl = CreateAuthorizationUrl(client, scope);

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, $"email profile {scope}");

        // Act
        var result = authenticationState.ValidateScopes(out var errorMessage);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedErrorMessage, errorMessage);
    }

    public static TheoryData<AuthenticationState, string> GetNextHopUrlData
    {
        get
        {
            var journeyId = Guid.NewGuid();
            var client = TestClients.Client1;
            var scope = "email profile trn";
            var authorizationUrl = CreateAuthorizationUrl(client, scope);

            return new TheoryData<AuthenticationState, string>()
            {
                // No email address
                {
                    new AuthenticationState(journeyId, authorizationUrl, client.ClientId!, scope)
                    {
                        EmailAddress = null
                    },
                    $"/sign-in/email?asid={journeyId}"
                },

                // Not confirmed email address
                {
                    new AuthenticationState(journeyId, authorizationUrl, client.ClientId!, scope)
                    {
                        EmailAddress = "john.doe@example.com"
                    },
                    $"/sign-in/email-confirmation?asid={journeyId}"
                },

                // Unknown user, not redirected to Find yet
                {
                    new AuthenticationState(journeyId, authorizationUrl, client.ClientId!, scope)
                    {
                        EmailAddress = "john.doe@example.com",
                        EmailAddressVerified = true,
                        FirstTimeUser = true,
                        HaveCompletedTrnLookup = false
                    },
                    $"/sign-in/trn?asid={journeyId}"
                },

                // Unknown user, has completed Find journey
                {
                    new AuthenticationState(journeyId, authorizationUrl, client.ClientId!, scope)
                    {
                        EmailAddress = "john.doe@example.com",
                        EmailAddressVerified = true,
                        FirstTimeUser = true,
                        HaveCompletedTrnLookup = true,
                        UserId = Guid.NewGuid()
                    },
                    authorizationUrl.SetQueryParam("asid", journeyId)
                },

                // Known user, confirmed
                {
                    new AuthenticationState(journeyId, authorizationUrl, client.ClientId!, scope)
                    {
                        EmailAddress = "john.doe@example.com",
                        EmailAddressVerified = true,
                        UserId = Guid.NewGuid(),
                        Trn = "1234567",
                        FirstTimeUser = false
                    },
                    authorizationUrl.SetQueryParam("asid", journeyId)
                },
            };
        }
    }

    public static TheoryData<string, UserType> GetUserTypeData => new TheoryData<string, UserType>
    {
        { CustomScopes.Trn, UserType.Teacher },
        { CustomScopes.GetAnIdentityAdmin, UserType.Admin },
        { CustomScopes.GetAnIdentitySupport, UserType.Admin }
    };

    public static TheoryData<string, bool, string?> ValidateClaimsData => new TheoryData<string, bool, string?>
    {
        { "", false, "The trn scope is required." },
        { CustomScopes.Trn, true, null },
        { CustomScopes.GetAnIdentityAdmin, true, null },
        { CustomScopes.GetAnIdentityAdmin + " " + CustomScopes.Trn, false, "The get-an-identity:admin, trn scopes cannot be combined." },
        { CustomScopes.GetAnIdentitySupport + " " + CustomScopes.Trn, false, "The get-an-identity:support, trn scopes cannot be combined." },
        { CustomScopes.GetAnIdentityAdmin + " " + CustomScopes.GetAnIdentitySupport, true, null },
    };

    private static string CreateAuthorizationUrl(OpenIddictApplicationDescriptor client, string scope)
    {
        var codeChallenge = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("12345")));

        var authorizationUrl = $"/connect/authorize" +
            $"?client_id={client.ClientId}" +
            $"&response_type=code" +
            $"&scope=" + Uri.EscapeDataString(scope) +
            $"&redirect_uri={Uri.EscapeDataString(client.RedirectUris.First().ToString())}" +
            $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
            $"&code_challenge_method=S256" +
            $"&response_mode=form_post";

        return authorizationUrl;
    }

    private class ClaimTypeAndValueEqualityComparer : IEqualityComparer<Claim>
    {
        public bool Equals(Claim? x, Claim? y)
        {
            return x is null && y is null ||
                (x is not null && y is not null && x.Type == y.Type && x.Value == y.Value);
        }

        public int GetHashCode([DisallowNull] Claim obj) => HashCode.Combine(obj.Type, obj.Value);
    }
}
