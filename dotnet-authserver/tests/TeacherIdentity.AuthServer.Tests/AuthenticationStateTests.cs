using System.Security.Claims;
using Flurl;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Tests.Infrastructure;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Tests;

public partial class AuthenticationStateTests
{
    [Fact]
    public void FromUser_DefaultUser_MapsDataOnCorrectly()
    {
        // Arrange
        var user = new User()
        {
            CompletedTrnLookup = new(2023, 2, 16, 18, 44, 17),
            Created = DateTime.UtcNow,
            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
            EmailAddress = Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            LastName = Faker.Name.Last(),
            Trn = "2345678",
            TrnAssociationSource = TrnAssociationSource.Lookup,
            TrnLookupStatus = TrnLookupStatus.Found,
            Updated = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            UserType = UserType.Default
        };

        var firstTimeSignInForEmail = true;

        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.DefaultUserType | UserRequirements.TrnHolder;

        // Act
        var authenticationState = AuthenticationState.FromUser(
            journeyId,
            userRequirements,
            user,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow,
            oAuthState: null,
            firstTimeSignInForEmail: firstTimeSignInForEmail);

        // Assert
        Assert.Equal(user.DateOfBirth, authenticationState.DateOfBirth);
        Assert.Equal(user.EmailAddress, authenticationState.EmailAddress);
        Assert.True(authenticationState.EmailAddressVerified);
        Assert.Equal(user.FirstName, authenticationState.FirstName);
        Assert.Equal(user.LastName, authenticationState.LastName);
        Assert.Equal(firstTimeSignInForEmail, authenticationState.FirstTimeSignInForEmail);
        Assert.True(authenticationState.HaveCompletedTrnLookup);
        Assert.Equal(user.Trn, authenticationState.Trn);
        Assert.Equal(user.UserId, authenticationState.UserId);
        Assert.Equal(AuthenticationState.TrnLookupState.Complete, authenticationState.TrnLookup);
        Assert.Equal(user.UserType, authenticationState.UserType);
        Assert.Equal(TrnLookupStatus.Found, authenticationState.TrnLookupStatus);
    }

    [Fact]
    public void FromUser_StaffUser_MapsDataOnCorrectly()
    {
        // Arrange
        var user = new User()
        {
            CompletedTrnLookup = new(2023, 2, 16, 18, 44, 17),
            Created = DateTime.UtcNow,
            EmailAddress = Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            LastName = Faker.Name.Last(),
            StaffRoles = new[] { StaffRoles.GetAnIdentityAdmin, StaffRoles.GetAnIdentitySupport },
            Updated = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            UserType = UserType.Default
        };

        var firstTimeSignInForEmail = true;

        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.DefaultUserType | UserRequirements.TrnHolder;

        // Act
        var authenticationState = AuthenticationState.FromUser(
            journeyId,
            userRequirements,
            user,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow,
            oAuthState: null,
            firstTimeSignInForEmail: firstTimeSignInForEmail);

        // Assert
        Assert.Equal(user.DateOfBirth, authenticationState.DateOfBirth);
        Assert.Equal(user.EmailAddress, authenticationState.EmailAddress);
        Assert.True(authenticationState.EmailAddressVerified);
        Assert.Equal(user.FirstName, authenticationState.FirstName);
        Assert.Equal(user.LastName, authenticationState.LastName);
        Assert.Equal(firstTimeSignInForEmail, authenticationState.FirstTimeSignInForEmail);
        Assert.Equal(user.StaffRoles, authenticationState.StaffRoles);
        Assert.Equal(user.UserId, authenticationState.UserId);
        Assert.Equal(user.UserType, authenticationState.UserType);
        Assert.Null(authenticationState.TrnLookupStatus);
    }

    [Fact]
    public void GetInternalClaims_DefaultUser_ReturnsExpectedClaims()
    {
        // Arrange
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var trn = "2345678";
        var userId = Guid.NewGuid();
        var userType = UserType.Default;
        var trnLookupStatus = TrnLookupStatus.Found;

        var user = new User()
        {
            DateOfBirth = dateOfBirth,
            CompletedTrnLookup = DateTime.UtcNow,
            Created = DateTime.UtcNow,
            EmailAddress = email,
            FirstName = firstName,
            LastName = lastName,
            Trn = trn,
            Updated = DateTime.UtcNow,
            UserId = userId,
            UserType = userType,
            TrnLookupStatus = trnLookupStatus
        };

        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.DefaultUserType | UserRequirements.TrnHolder;

        var authenticationState = new AuthenticationState(
            journeyId: Guid.NewGuid(),
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow);

        authenticationState.OnEmailSet(email);
        authenticationState.OnEmailVerified(user);

        // Act
        var claims = authenticationState.GetInternalClaims();

        // Assert
        var expectedClaims = new[]
        {
            new Claim(Claims.Subject, userId.ToString()!),
            new Claim(Claims.Email, email),
            new Claim(Claims.Name, firstName + " " + lastName),
            new Claim(Claims.GivenName, firstName),
            new Claim(Claims.FamilyName, lastName),
            new Claim(CustomClaims.Trn, trn),
            new Claim(CustomClaims.UserType, userType.ToString())
        };
        Assert.Equal(expectedClaims.OrderBy(c => c.Type), claims.OrderBy(c => c.Type), new ClaimTypeAndValueEqualityComparer());
    }

    [Fact]
    public void GetInternalClaims_StaffUser_ReturnsExpectedClaims()
    {
        // Arrange
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var userId = Guid.NewGuid();
        var userType = UserType.Staff;
        var staffRoles = new[] { StaffRoles.GetAnIdentityAdmin, StaffRoles.GetAnIdentitySupport };

        var user = new User()
        {
            DateOfBirth = dateOfBirth,
            Created = DateTime.UtcNow,
            EmailAddress = email,
            FirstName = firstName,
            LastName = lastName,
            StaffRoles = staffRoles,
            Updated = DateTime.UtcNow,
            UserId = userId,
            UserType = userType
        };

        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.StaffUserType;

        var authenticationState = new AuthenticationState(
            journeyId: Guid.NewGuid(),
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow);

        authenticationState.OnEmailSet(email);
        authenticationState.OnEmailVerified(user);

        // Act
        var claims = authenticationState.GetInternalClaims();

        // Assert
        var expectedClaims = new[]
        {
            new Claim(Claims.Subject, userId.ToString()!),
            new Claim(Claims.Email, email),
            new Claim(Claims.Name, firstName + " " + lastName),
            new Claim(Claims.GivenName, firstName),
            new Claim(Claims.FamilyName, lastName),
            new Claim(Claims.Role, StaffRoles.GetAnIdentityAdmin),
            new Claim(Claims.Role, StaffRoles.GetAnIdentitySupport),
            new Claim(CustomClaims.UserType, userType.ToString())
        };
        Assert.Equal(expectedClaims.OrderBy(c => c.Type), claims.OrderBy(c => c.Type), new ClaimTypeAndValueEqualityComparer());
    }

    [Theory]
    [MemberData(nameof(GetNextHopUrlAndLastMilestoneData))]
    public void GetNextHopUrlAndLastMilestone(
        AuthenticationState authenticationState,
        string expectedNextHopUrl,
        AuthenticationState.AuthenticationMilestone expectedMilestone)
    {
        // Arrange
        var linkGenerator = new Mock<LinkGenerator>();

        var identityLinkGenerator = new TestIdentityLinkGenerator(
            authenticationState,
            linkGenerator.Object,
            new Helpers.QueryStringSignatureHelper(key: "dummy"));

        void ConfigureMockForPage(string pageName, string returnsPath)
        {
            linkGenerator
                .Setup(mock => mock.GetPathByAddress<RouteValuesAddress>(
                    It.Is<RouteValuesAddress>(r => r.ExplicitValues["page"]!.Equals(pageName)),
                    It.IsAny<RouteValueDictionary>(),
                    It.IsAny<PathString>(),
                    It.IsAny<FragmentString>(),
                    It.IsAny<LinkOptions>()))
                .Returns(returnsPath.SetQueryParam("asid", authenticationState.JourneyId.ToString()));
        }

        ConfigureMockForPage("/SignIn/Landing", "/sign-in/landing");
        ConfigureMockForPage("/SignIn/Email", "/sign-in/email");
        ConfigureMockForPage("/SignIn/EmailConfirmation", "/sign-in/email-confirmation");
        ConfigureMockForPage("/SignIn/Trn", "/sign-in/trn");
        ConfigureMockForPage("/SignIn/TrnCallback", "/sign-in/trn/callback");
        ConfigureMockForPage("/SignIn/TrnInUse", "/sign-in/trn/different-email");
        ConfigureMockForPage("/SignIn/TrnInUseChooseEmail", "/sign-in/trn/choose-email");
        ConfigureMockForPage("/SignIn/Register/Phone", "/sign-in/register/phone");

        // Act
        var nextHopUrl = authenticationState.GetNextHopUrl(identityLinkGenerator);
        var lastMilestone = authenticationState.GetLastMilestone();

        // Assert
        Assert.Equal(expectedNextHopUrl, nextHopUrl);
        Assert.Equal(expectedMilestone, lastMilestone);
    }

    [Fact]
    public void OnEmailSet()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.DefaultUserType;

        var email = Faker.Internet.Email();

        var authenticationState = new AuthenticationState(
            journeyId,
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow);

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
        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.DefaultUserType;

        var email = Faker.Internet.Email();

        var authenticationState = new AuthenticationState(
            journeyId,
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow);

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
        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.StaffUserType;

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
            Updated = DateTime.UtcNow,
            UserId = userId,
            UserType = UserType.Staff
        };

        var authenticationState = new AuthenticationState(
            journeyId,
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow);

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
        Assert.Equal(UserType.Staff, authenticationState.UserType);
    }

    [Fact]
    public void OnEmailVerified_WithUserWhoHasCompletedTrnLookup()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.DefaultUserType | UserRequirements.TrnHolder;

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
            Updated = DateTime.UtcNow,
            UserId = userId,
            UserType = UserType.Default
        };

        var authenticationState = new AuthenticationState(
            journeyId,
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow);

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
        Assert.Equal(UserType.Default, authenticationState.UserType);
    }

    [Fact]
    public void OnEmailVerified_WithUserWhoHasTrnAssignedViaApi()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.DefaultUserType | UserRequirements.TrnHolder;

        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var trn = "2345678";
        var userId = Guid.NewGuid();

        var user = new User()
        {
            DateOfBirth = dateOfBirth,
            CompletedTrnLookup = null,
            Created = DateTime.UtcNow,
            EmailAddress = email,
            FirstName = firstName,
            LastName = lastName,
            Trn = trn,
            TrnAssociationSource = TrnAssociationSource.Api,
            TrnLookupStatus = TrnLookupStatus.Found,
            Updated = DateTime.UtcNow,
            UserId = userId,
            UserType = UserType.Default
        };

        var authenticationState = new AuthenticationState(
            journeyId,
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow);

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
        Assert.False(authenticationState.HaveCompletedTrnLookup);
        Assert.Equal(AuthenticationState.TrnLookupState.Complete, authenticationState.TrnLookup);
        Assert.Equal(UserType.Default, authenticationState.UserType);
    }

    [Fact]
    public void OnTrnLookupCompletedAndUserRegistered()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.DefaultUserType | UserRequirements.TrnHolder;

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
            Updated = DateTime.UtcNow,
            UserId = userId,
            UserType = UserType.Default
        };

        var authenticationState = new AuthenticationState(
            journeyId,
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow);

        authenticationState.OnEmailSet(email);
        authenticationState.OnEmailVerified(user: null);

        // Act
        authenticationState.OnTrnLookupCompletedAndUserRegistered(user);

        // Assert
        Assert.True(authenticationState.EmailAddressVerified);
        Assert.True(authenticationState.FirstTimeSignInForEmail);
        Assert.Equal(dateOfBirth, authenticationState.DateOfBirth);
        Assert.Equal(firstName, authenticationState.FirstName);
        Assert.Equal(lastName, authenticationState.LastName);
        Assert.Equal(trn, authenticationState.Trn);
        Assert.Equal(userId, authenticationState.UserId);
        Assert.True(authenticationState.HaveCompletedTrnLookup);
        Assert.Equal(AuthenticationState.TrnLookupState.Complete, authenticationState.TrnLookup);
        Assert.Equal(UserType.Default, authenticationState.UserType);
    }

    [Fact]
    public void OnTrnLookupCompletedForTrnAlreadyInUse()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.DefaultUserType | UserRequirements.TrnHolder;

        var email = Faker.Internet.Email();
        var existingTrnOwnerEmail = Faker.Internet.Email();

        var authenticationState = new AuthenticationState(
            journeyId,
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow);

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
        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.DefaultUserType | UserRequirements.TrnHolder;

        var email = Faker.Internet.Email();
        var existingTrnOwnerEmail = Faker.Internet.Email();

        var authenticationState = new AuthenticationState(
            journeyId,
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow);

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
        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.DefaultUserType | UserRequirements.TrnHolder;

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
            Updated = DateTime.UtcNow,
            UserId = userId,
            UserType = UserType.Default
        };

        var authenticationState = new AuthenticationState(
            journeyId,
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow);

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
        Assert.Equal(UserType.Default, authenticationState.UserType);
    }

    [Fact]
    public void ResolveServiceUrl_ApplicationHasAbsoluteServiceUrl_ReturnsServiceUrl()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.DefaultUserType;
        var redirectUri = "http://client.com/redirect_uri";
        var client = new Application()
        {
            RedirectUris = $"[ \"{redirectUri}\" ]",
            ServiceUrl = "http://other-domain.com/start"
        };

        var fullScope = "email profile";

        var authenticationState = new AuthenticationState(
            journeyId,
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow,
            oAuthState: new OAuthAuthorizationState(client.ClientId!, fullScope, redirectUri));

        // Act
        var result = authenticationState.OAuthState!.ResolveServiceUrl(client);

        // Assert
        Assert.Equal(client.ServiceUrl, result);
    }

    [Fact]
    public void ResolveServiceUrl_ApplicationHasRelativeServiceUrl_ReturnsUrlRelativeToRedirectUri()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var userRequirements = UserRequirements.DefaultUserType;
        var redirectUri = "http://client.com/redirect_uri";
        var client = new Application()
        {
            RedirectUris = $"[ \"{redirectUri}\" ]",
            ServiceUrl = "/start"
        };

        var fullScope = "email profile";

        var authenticationState = new AuthenticationState(
            journeyId,
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow,
            oAuthState: new OAuthAuthorizationState(client.ClientId!, fullScope, redirectUri));

        // Act
        var result = authenticationState.OAuthState!.ResolveServiceUrl(client);

        // Assert
        Assert.Equal("http://client.com/start", result);
    }

    public static TheoryData<AuthenticationState, string, AuthenticationState.AuthenticationMilestone> GetNextHopUrlAndLastMilestoneData
    {
        get
        {
            var journeyId = Guid.NewGuid();
            var clientId = "test";
            var postSignInUrl = "/callback";

            var oAuthStateRequiringLegacyTrnLookup = new OAuthAuthorizationState(
                clientId,
                CustomScopes.DqtRead,
                redirectUri: "https://example.com?callback")
            {
                TrnRequirementType = TrnRequirementType.Legacy
            };

            // Helper method for creating an AuthenticationState object and modifying it via its On* methods
            static AuthenticationState S(AuthenticationState state, params Action<AuthenticationState>[] configure)
            {
                foreach (var c in configure)
                {
                    c(state);
                }

                return state;
            }

            return new TheoryData<AuthenticationState, string, AuthenticationState.AuthenticationMilestone>()
            {
                // No email address
                {
                    S(new AuthenticationState(journeyId, UserRequirements.DefaultUserType, postSignInUrl, startedAt: DateTime.UtcNow)),
                    $"/sign-in/landing?asid={journeyId}",
                    AuthenticationState.AuthenticationMilestone.None
                },

                // No email address (legacy TRN journey)
                {
                    S(new AuthenticationState(journeyId, UserRequirements.DefaultUserType, postSignInUrl, startedAt: DateTime.UtcNow, oAuthState: oAuthStateRequiringLegacyTrnLookup)),
                    $"/sign-in/email?asid={journeyId}",
                    AuthenticationState.AuthenticationMilestone.None
                },

                // Got an email but not yet verified
                {
                    S(
                        new AuthenticationState(journeyId, UserRequirements.DefaultUserType, postSignInUrl, startedAt: DateTime.UtcNow),
                        s => s.OnEmailSet("john.doe@example.com")
                    ),
                    $"/sign-in/email-confirmation?asid={journeyId}",
                    AuthenticationState.AuthenticationMilestone.None
                },

                // New user with verified email
                {
                    S(
                        new AuthenticationState(journeyId, UserRequirements.DefaultUserType, postSignInUrl, startedAt: DateTime.UtcNow),
                        s => s.OnEmailSet("john.doe@example.com"),
                        s => s.OnEmailVerified(user: null)
                    ),
                    $"/sign-in/register/phone?asid={journeyId}",
                    AuthenticationState.AuthenticationMilestone.EmailVerified
                },

                // New user with verified email, not completed TRN lookup yet (legacy TRN journey)
                {
                    S(
                        new AuthenticationState(journeyId, UserRequirements.DefaultUserType | UserRequirements.TrnHolder, postSignInUrl, startedAt: DateTime.UtcNow, oAuthState: oAuthStateRequiringLegacyTrnLookup),
                        s => s.OnEmailSet("john.doe@example.com"),
                        s => s.OnEmailVerified(user: null)
                    ),
                    $"/sign-in/trn?asid={journeyId}",
                    AuthenticationState.AuthenticationMilestone.EmailVerified
                },

                // New user who has completed TRN lookup
                {
                    S(
                        new AuthenticationState(journeyId, UserRequirements.DefaultUserType | UserRequirements.TrnHolder, postSignInUrl, startedAt: DateTime.UtcNow),
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
                            Updated = DateTime.UtcNow,
                            UserId = Guid.NewGuid(),
                            UserType = UserType.Default
                        })
                    ),
                    postSignInUrl,
                    AuthenticationState.AuthenticationMilestone.Complete
                },

                // New user who has completed TRN lookup with an already-assigned TRN
                {
                    S(
                        new AuthenticationState(journeyId, UserRequirements.DefaultUserType | UserRequirements.TrnHolder, postSignInUrl, startedAt: DateTime.UtcNow),
                        s => s.OnEmailSet("john.doe@example.com"),
                        s => s.OnEmailVerified(user: null),
                        s => s.OnTrnLookupCompletedForTrnAlreadyInUse(existingTrnOwnerEmail: Faker.Internet.Email())
                    ),
                    $"/sign-in/trn/different-email?asid={journeyId}",
                    AuthenticationState.AuthenticationMilestone.TrnLookupCompleted
                },

                // New user who has completed TRN lookup with an already-assigned TRN and they've verified that account's email
                {
                    S(
                        new AuthenticationState(journeyId, UserRequirements.DefaultUserType | UserRequirements.TrnHolder, postSignInUrl, startedAt: DateTime.UtcNow),
                        s => s.OnEmailSet("john.doe@example.com"),
                        s => s.OnEmailVerified(user: null),
                        s => s.OnTrnLookupCompletedForTrnAlreadyInUse(existingTrnOwnerEmail: Faker.Internet.Email()),
                        s => s.OnEmailVerifiedOfExistingAccountForTrn()
                    ),
                    $"/sign-in/trn/choose-email?asid={journeyId}",
                    AuthenticationState.AuthenticationMilestone.TrnLookupCompleted
                },

                // New user who has completed TRN lookup with an already-assigned TRN and they've choosen the email to use
                {
                    S(
                        new AuthenticationState(journeyId, UserRequirements.DefaultUserType | UserRequirements.TrnHolder, postSignInUrl, startedAt: DateTime.UtcNow),
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
                            Updated = DateTime.UtcNow,
                            UserId = Guid.NewGuid(),
                            UserType = UserType.Default,
                            Trn = "2345678"
                        })
                    ),
                    postSignInUrl,
                    AuthenticationState.AuthenticationMilestone.Complete
                },

                // Existing user
                {
                    S(
                        new AuthenticationState(journeyId, UserRequirements.DefaultUserType | UserRequirements.TrnHolder, postSignInUrl, startedAt: DateTime.UtcNow),
                        s => s.OnEmailSet("john.doe@example.com"),
                        s => s.OnEmailVerified(new User()
                        {
                            CompletedTrnLookup = DateTime.UtcNow,
                            Created = DateTime.UtcNow,
                            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                            FirstName = Faker.Name.First(),
                            LastName = Faker.Name.Last(),
                            EmailAddress = "john.doe@example.com",
                            Updated = DateTime.UtcNow,
                            UserId = Guid.NewGuid(),
                            UserType = UserType.Default
                        })
                    ),
                    postSignInUrl,
                    AuthenticationState.AuthenticationMilestone.Complete
                },

                // Existing user who has had TRN assigned via API
                {
                    S(
                        new AuthenticationState(journeyId, UserRequirements.DefaultUserType | UserRequirements.TrnHolder, postSignInUrl, startedAt: DateTime.UtcNow),
                        s => s.OnEmailSet("john.doe@example.com"),
                        s => s.OnEmailVerified(new User()
                        {
                            CompletedTrnLookup = null,
                            Created = DateTime.UtcNow,
                            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                            FirstName = Faker.Name.First(),
                            LastName = Faker.Name.Last(),
                            EmailAddress = "john.doe@example.com",
                            Updated = DateTime.UtcNow,
                            UserId = Guid.NewGuid(),
                            UserType = UserType.Default,
                            Trn = "2345678",
                            TrnAssociationSource = TrnAssociationSource.Api,
                            TrnLookupStatus = TrnLookupStatus.Found
                        })
                    ),
                    postSignInUrl,
                    AuthenticationState.AuthenticationMilestone.Complete
                },
            };
        }
    }
}
