using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Flurl;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Tests;

public partial class AuthenticationStateTests
{
    [Fact]
    public void FromInternalClaims()
    {
        // Arrange
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var email = Faker.Internet.Email();
        var emailVerified = true;
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var firstTimeSignInForEmail = true;
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
        var redirectUri = client.RedirectUris.First().ToString();
        var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, scope, redirectUri);

        // Act
        var authenticationState = AuthenticationState.FromInternalClaims(claims, authorizationUrl, client.ClientId!, scope, redirectUri, firstTimeSignInForEmail);

        // Assert
        Assert.Equal(dateOfBirth, authenticationState.DateOfBirth);
        Assert.Equal(email, authenticationState.EmailAddress);
        Assert.Equal(emailVerified, authenticationState.EmailAddressVerified);
        Assert.Equal(firstName, authenticationState.FirstName);
        Assert.Equal(lastName, authenticationState.LastName);
        Assert.Equal(firstTimeSignInForEmail, authenticationState.FirstTimeSignInForEmail);
        Assert.Equal(haveCompletedTrnLookup, authenticationState.HaveCompletedTrnLookup);
        Assert.Equal(trn, authenticationState.Trn);
        Assert.Equal(userId, authenticationState.UserId);
        Assert.Equal(AuthenticationState.TrnLookupState.Complete, authenticationState.TrnLookup);
    }

    [Fact]
    public void GetInternalClaims()
    {
        // Arrange
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var email = Faker.Internet.Email();
        var emailVerified = true;
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var haveCompletedTrnLookup = true;
        var trn = "2345678";
        var userId = Guid.NewGuid();

        var user = new User()
        {
            DateOfBirth = dateOfBirth,
            CompletedTrnLookup = DateTime.UtcNow,
            Created = DateTime.UtcNow,
            EmailAddress = email,
            FirstName = firstName,
            LastName = lastName,
            Trn = trn,
            UserId = userId,
            UserType = UserType.Default
        };

        var client = TestClients.Client1;
        var scope = "email profile trn";
        var redirectUri = client.RedirectUris.First().ToString();
        var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, scope, redirectUri);

        var authenticationState = new AuthenticationState(journeyId: Guid.NewGuid(), authorizationUrl, client.ClientId!, scope, redirectUri);
        authenticationState.OnEmailSet(email);
        authenticationState.OnEmailVerified(user);

        // Act
        var claims = authenticationState.GetInternalClaims();

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
        var linkGenerator = new Mock<IIdentityLinkGenerator>();

        void ConfigureMockForPage(string pageName, string returnsPath)
        {
            linkGenerator.Setup(mock => mock.PageWithAuthenticationJourneyId(pageName))
                .Returns(returnsPath.SetQueryParam("asid", authenticationState.JourneyId.ToString()));
        }

        ConfigureMockForPage("/SignIn/Email", "/sign-in/email");
        ConfigureMockForPage("/SignIn/EmailConfirmation", "/sign-in/email-confirmation");
        ConfigureMockForPage("/SignIn/Trn", "/sign-in/trn");
        ConfigureMockForPage("/SignIn/TrnCallback", "/sign-in/trn/callback");
        ConfigureMockForPage("/SignIn/TrnInUse", "/sign-in/trn/different-email");
        ConfigureMockForPage("/SignIn/TrnInUseChooseEmail", "/sign-in/trn/choose-email");

        // Act
        var result = authenticationState.GetNextHopUrl(linkGenerator.Object);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void OnEmailSet()
    {
        // Arrange
        var client = TestClients.Client1;
        var scope = "email profile trn";
        var redirectUri = client.RedirectUris.First().ToString();
        var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, scope, redirectUri);

        var email = Faker.Internet.Email();

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, scope, redirectUri);

        // Act
        authenticationState.OnEmailSet(email);

        // Assert
        Assert.Equal(email, authenticationState.EmailAddress);
        Assert.False(authenticationState.EmailAddressVerified);
    }

    [Fact]
    public void OnEmailVerified_WithoutUser()
    {
        // Arrange
        var client = TestClients.Client1;
        var scope = "email profile trn";
        var redirectUri = client.RedirectUris.First().ToString();
        var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, scope, redirectUri);

        var email = Faker.Internet.Email();

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, scope, redirectUri);
        authenticationState.OnEmailSet(email);

        // Act
        authenticationState.OnEmailVerified(user: null);

        // Assert
        Assert.True(authenticationState.EmailAddressVerified);
        Assert.True(authenticationState.FirstTimeSignInForEmail);
    }

    [Fact]
    public void OnEmailVerified_WithStaffUser()
    {
        // Arrange
        var client = TestClients.Client1;
        var scope = $"email profile {CustomScopes.GetAnIdentityAdmin}";
        var redirectUri = client.RedirectUris.First().ToString();
        var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, scope, redirectUri);

        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var userId = Guid.NewGuid();

        var user = new User()
        {
            DateOfBirth = dateOfBirth,
            Created = DateTime.UtcNow,
            EmailAddress = email,
            FirstName = firstName,
            LastName = lastName,
            UserId = userId,
            UserType = UserType.Staff
        };

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, scope, redirectUri);
        authenticationState.OnEmailSet(email);

        // Act
        authenticationState.OnEmailVerified(user);

        // Assert
        Assert.True(authenticationState.EmailAddressVerified);
        Assert.False(authenticationState.FirstTimeSignInForEmail);
        Assert.Equal(dateOfBirth, authenticationState.DateOfBirth);
        Assert.Equal(firstName, authenticationState.FirstName);
        Assert.Equal(lastName, authenticationState.LastName);
        Assert.Null(authenticationState.Trn);
        Assert.Equal(userId, authenticationState.UserId);
        Assert.False(authenticationState.HaveCompletedTrnLookup);
    }

    [Fact]
    public void OnEmailVerified_WithUserWhoHasCompletedTrnLookup()
    {
        // Arrange
        var client = TestClients.Client1;
        var scope = "email profile trn";
        var redirectUri = client.RedirectUris.First().ToString();
        var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, scope, redirectUri);

        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var trn = "2345678";
        var userId = Guid.NewGuid();

        var user = new User()
        {
            DateOfBirth = dateOfBirth,
            CompletedTrnLookup = DateTime.UtcNow,
            Created = DateTime.UtcNow,
            EmailAddress = email,
            FirstName = firstName,
            LastName = lastName,
            Trn = trn,
            UserId = userId,
            UserType = UserType.Default
        };

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, scope, redirectUri);
        authenticationState.OnEmailSet(email);

        // Act
        authenticationState.OnEmailVerified(user);

        // Assert
        Assert.True(authenticationState.EmailAddressVerified);
        Assert.False(authenticationState.FirstTimeSignInForEmail);
        Assert.Equal(dateOfBirth, authenticationState.DateOfBirth);
        Assert.Equal(firstName, authenticationState.FirstName);
        Assert.Equal(lastName, authenticationState.LastName);
        Assert.Equal(trn, authenticationState.Trn);
        Assert.Equal(userId, authenticationState.UserId);
        Assert.True(authenticationState.HaveCompletedTrnLookup);
        Assert.Equal(AuthenticationState.TrnLookupState.Complete, authenticationState.TrnLookup);
    }

    [Fact]
    public void OnTrnLookupCompletedAndUserRegistered()
    {
        // Arrange
        var client = TestClients.Client1;
        var scope = "email profile trn";
        var redirectUri = client.RedirectUris.First().ToString();
        var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, scope, redirectUri);

        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var trn = "2345678";
        var userId = Guid.NewGuid();

        var user = new User()
        {
            DateOfBirth = dateOfBirth,
            CompletedTrnLookup = DateTime.UtcNow,
            Created = DateTime.UtcNow,
            EmailAddress = email,
            FirstName = firstName,
            LastName = lastName,
            Trn = trn,
            UserId = userId,
            UserType = UserType.Default
        };
        var firstTimeSignInForEmail = true;

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, scope, redirectUri);
        authenticationState.OnEmailSet(email);
        authenticationState.OnEmailVerified(user: null);

        // Act
        authenticationState.OnTrnLookupCompletedAndUserRegistered(user, firstTimeSignInForEmail);

        // Assert
        Assert.True(authenticationState.EmailAddressVerified);
        Assert.Equal(firstTimeSignInForEmail, authenticationState.FirstTimeSignInForEmail);
        Assert.Equal(dateOfBirth, authenticationState.DateOfBirth);
        Assert.Equal(firstName, authenticationState.FirstName);
        Assert.Equal(lastName, authenticationState.LastName);
        Assert.Equal(trn, authenticationState.Trn);
        Assert.Equal(userId, authenticationState.UserId);
        Assert.True(authenticationState.HaveCompletedTrnLookup);
        Assert.Equal(AuthenticationState.TrnLookupState.Complete, authenticationState.TrnLookup);
    }

    [Fact]
    public void OnTrnLookupCompletedForTrnAlreadyInUse()
    {
        // Arrange
        var client = TestClients.Client1;
        var scope = "email profile trn";
        var redirectUri = client.RedirectUris.First().ToString();
        var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, scope, redirectUri);

        var email = Faker.Internet.Email();
        var existingTrnOwnerEmail = Faker.Internet.Email();

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, scope, redirectUri);
        authenticationState.OnEmailSet(email);
        authenticationState.OnEmailVerified(user: null);

        // Act
        authenticationState.OnTrnLookupCompletedForTrnAlreadyInUse(existingTrnOwnerEmail);

        // Assert
        Assert.True(authenticationState.HaveCompletedTrnLookup);
        Assert.Equal(AuthenticationState.TrnLookupState.ExistingTrnFound, authenticationState.TrnLookup);
        Assert.Equal(existingTrnOwnerEmail, authenticationState.TrnOwnerEmailAddress);
    }

    [Fact]
    public void OnEmailVerifiedOfExistingAccountForTrn()
    {
        // Arrange
        var client = TestClients.Client1;
        var scope = "email profile trn";
        var redirectUri = client.RedirectUris.First().ToString();
        var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, scope, redirectUri);

        var email = Faker.Internet.Email();
        var existingTrnOwnerEmail = Faker.Internet.Email();

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, scope, redirectUri);
        authenticationState.OnEmailSet(email);
        authenticationState.OnEmailVerified(user: null);
        authenticationState.OnTrnLookupCompletedForTrnAlreadyInUse(existingTrnOwnerEmail);

        // Act
        authenticationState.OnEmailVerifiedOfExistingAccountForTrn();

        // Assert
        Assert.Equal(AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified, authenticationState.TrnLookup);
    }

    [Fact]
    public void OnEmailAddressChosen()
    {
        // Arrange
        var client = TestClients.Client1;
        var scope = "email profile trn";
        var redirectUri = client.RedirectUris.First().ToString();
        var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, scope, redirectUri);

        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var trn = "2345678";
        var userId = Guid.NewGuid();
        var existingTrnOwnerEmail = Faker.Internet.Email();

        var user = new User()
        {
            DateOfBirth = dateOfBirth,
            CompletedTrnLookup = DateTime.UtcNow,
            Created = DateTime.UtcNow,
            EmailAddress = email,
            FirstName = firstName,
            LastName = lastName,
            Trn = trn,
            UserId = userId,
            UserType = UserType.Default
        };

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, scope, redirectUri);
        authenticationState.OnEmailSet(email);
        authenticationState.OnEmailVerified(user: null);
        authenticationState.OnTrnLookupCompletedForTrnAlreadyInUse(existingTrnOwnerEmail);
        authenticationState.OnEmailVerifiedOfExistingAccountForTrn();

        // Act
        authenticationState.OnEmailAddressChosen(user);

        // Assert
        Assert.True(authenticationState.FirstTimeSignInForEmail);
        Assert.Equal(dateOfBirth, authenticationState.DateOfBirth);
        Assert.Equal(firstName, authenticationState.FirstName);
        Assert.Equal(lastName, authenticationState.LastName);
        Assert.Equal(trn, authenticationState.Trn);
        Assert.Equal(userId, authenticationState.UserId);
        Assert.Equal(AuthenticationState.TrnLookupState.Complete, authenticationState.TrnLookup);
    }

    [Theory]
    [MemberData(nameof(GetUserTypeData))]
    public void GetUserType(string scope, UserType expectedUserType)
    {
        // Arrange
        var client = TestClients.Client1;
        var redirectUri = client.RedirectUris.First().ToString();
        var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, scope, redirectUri);

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, $"email profile {scope}", redirectUri);

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
        var redirectUri = client.RedirectUris.First().ToString();
        var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, scope, redirectUri);

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, $"email profile {scope}", redirectUri);

        // Act
        var result = authenticationState.ValidateScopes(out var errorMessage);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedErrorMessage, errorMessage);
    }

    [Fact]
    public void ResolveServiceUrl_ApplicationHasAbsoluteServiceUrl_ReturnsServiceUrl()
    {
        // Arrange
        var redirectUri = "http://client.com/redirect_uri";
        var client = new Application()
        {
            RedirectUris = $"[ \"{redirectUri}\" ]",
            ServiceUrl = "http://other-domain.com/start"
        };

        var fullScope = "email profile";
        var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, fullScope, redirectUri);

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, fullScope, redirectUri);

        // Act
        var result = authenticationState.ResolveServiceUrl(client);

        // Assert
        Assert.Equal(client.ServiceUrl, result);
    }

    [Fact]
    public void ResolveServiceUrl_ApplicationHasRelativeServiceUrl_ReturnsUrlRelativeToRedirectUri()
    {
        // Arrange
        var redirectUri = "http://client.com/redirect_uri";
        var client = new Application()
        {
            RedirectUris = $"[ \"{redirectUri}\" ]",
            ServiceUrl = "/start"
        };

        var fullScope = "email profile";
        var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, fullScope, redirectUri);

        var authenticationState = new AuthenticationState(Guid.NewGuid(), authorizationUrl, client.ClientId!, fullScope, redirectUri);

        // Act
        var result = authenticationState.ResolveServiceUrl(client);

        // Assert
        Assert.Equal("http://client.com/start", result);
    }

    public static TheoryData<AuthenticationState, string> GetNextHopUrlData
    {
        get
        {
            var journeyId = Guid.NewGuid();
            var client = TestClients.Client1;
            var scope = "email profile trn";
            var redirectUri = client.RedirectUris.First().ToString();
            var authorizationUrl = CreateAuthorizationUrl(client.ClientId!, scope, redirectUri);

            // Helper method for creating an AuthenticationState object and modifying it via its On* methods
            static AuthenticationState S(AuthenticationState state, params Action<AuthenticationState>[] configure)
            {
                foreach (var c in configure)
                {
                    c(state);
                }

                return state;
            }

            return new TheoryData<AuthenticationState, string>()
            {
                // No email address
                {
                    S(new AuthenticationState(journeyId, authorizationUrl, client.ClientId!, scope, redirectUri)),
                    $"/sign-in/email?asid={journeyId}"
                },

                // Got an email but not yet verified
                {
                    S(
                        new AuthenticationState(journeyId, authorizationUrl, client.ClientId!, scope, redirectUri),
                        s => s.OnEmailSet("john.doe@example.com")
                    ),
                    $"/sign-in/email-confirmation?asid={journeyId}"
                },

                // Verified email, not completed TRN lookup yet
                {
                    S(
                        new AuthenticationState(journeyId, authorizationUrl, client.ClientId!, scope, redirectUri),
                        s => s.OnEmailSet("john.doe@example.com"),
                        s => s.OnEmailVerified(user: null)
                    ),
                    $"/sign-in/trn?asid={journeyId}"
                },

                // New user who has completed TRN lookup
                {
                    S(
                        new AuthenticationState(journeyId, authorizationUrl, client.ClientId!, scope, redirectUri),
                        s => s.OnEmailSet("john.doe@example.com"),
                        s => s.OnEmailVerified(user: null),
                        s => s.OnTrnLookupCompletedAndUserRegistered(new User()
                        {
                            CompletedTrnLookup = DateTime.UtcNow,
                            Created = DateTime.UtcNow,
                            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                            FirstName = Faker.Name.First(),
                            LastName = Faker.Name.Last(),
                            EmailAddress = "john.doe@example.com",
                            UserId = Guid.NewGuid(),
                            UserType = UserType.Default
                        }, firstTimeSignInForEmail: true)
                    ),
                    authorizationUrl.SetQueryParam("asid", journeyId)
                },

                // New user who has completed TRN lookup with an already-assigned TRN
                {
                    S(
                        new AuthenticationState(journeyId, authorizationUrl, client.ClientId!, scope, redirectUri),
                        s => s.OnEmailSet("john.doe@example.com"),
                        s => s.OnEmailVerified(user: null),
                        s => s.OnTrnLookupCompletedForTrnAlreadyInUse(existingTrnOwnerEmail: Faker.Internet.Email())
                    ),
                    $"/sign-in/trn/different-email?asid={journeyId}"
                },

                // New user who has completed TRN lookup with an already-assigned TRN and they've verified that account's email
                {
                    S(
                        new AuthenticationState(journeyId, authorizationUrl, client.ClientId!, scope, redirectUri),
                        s => s.OnEmailSet("john.doe@example.com"),
                        s => s.OnEmailVerified(user: null),
                        s => s.OnTrnLookupCompletedForTrnAlreadyInUse(existingTrnOwnerEmail: Faker.Internet.Email()),
                        s => s.OnEmailVerifiedOfExistingAccountForTrn()
                    ),
                    $"/sign-in/trn/choose-email?asid={journeyId}"
                },

                // New user who has completed TRN lookup with an already-assigned TRN and they've choosen the email to use
                {
                    S(
                        new AuthenticationState(journeyId, authorizationUrl, client.ClientId!, scope, redirectUri),
                        s => s.OnEmailSet("john.doe@example.com"),
                        s => s.OnEmailVerified(user: null),
                        s => s.OnTrnLookupCompletedForTrnAlreadyInUse(existingTrnOwnerEmail: Faker.Internet.Email()),
                        s => s.OnEmailVerifiedOfExistingAccountForTrn(),
                        s => s.OnEmailAddressChosen(new User()
                        {
                            CompletedTrnLookup = DateTime.UtcNow,
                            Created = DateTime.UtcNow,
                            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                            FirstName = Faker.Name.First(),
                            LastName = Faker.Name.Last(),
                            EmailAddress = Faker.Internet.Email(),
                            UserId = Guid.NewGuid(),
                            UserType = UserType.Default,
                            Trn = "2345678"
                        })
                    ),
                    authorizationUrl.SetQueryParam("asid", journeyId)
                },

                // Existing user
                {
                    S(
                        new AuthenticationState(journeyId, authorizationUrl, client.ClientId!, scope, redirectUri),
                        s => s.OnEmailSet("john.doe@example.com"),
                        s => s.OnEmailVerified(new User()
                        {
                            CompletedTrnLookup = DateTime.UtcNow,
                            Created = DateTime.UtcNow,
                            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                            FirstName = Faker.Name.First(),
                            LastName = Faker.Name.Last(),
                            EmailAddress = "john.doe@example.com",
                            UserId = Guid.NewGuid(),
                            UserType = UserType.Default
                        })
                    ),
                    authorizationUrl.SetQueryParam("asid", journeyId)
                },
            };
        }
    }

    public static TheoryData<string, UserType> GetUserTypeData => new TheoryData<string, UserType>
    {
        { CustomScopes.Trn, UserType.Default },
        { CustomScopes.GetAnIdentityAdmin, UserType.Staff },
        { CustomScopes.GetAnIdentitySupport, UserType.Staff }
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

    private static string CreateAuthorizationUrl(string clientId, string scope, string redirectUri)
    {
        var codeChallenge = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("12345")));

        var authorizationUrl = $"/connect/authorize" +
            $"?client_id={clientId}" +
            $"&response_type=code" +
            $"&scope=" + Uri.EscapeDataString(scope) +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
            $"&code_challenge_method=S256" +
            $"&response_mode=form_post";

        return authorizationUrl;
    }
}
