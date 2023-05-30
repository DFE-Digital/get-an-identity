using System.Security.Claims;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Tests;

public partial class AuthenticationStateTests
{
    [Fact]
    public void FromUser_DefaultUser_MapsDataToAuthStateCorrectly()
    {
        // Arrange
        var user = new User()
        {
            CompletedTrnLookup = new(2023, 2, 16, 18, 44, 17),
            Created = DateTime.UtcNow,
            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
            EmailAddress = Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            MiddleName = Faker.Name.Middle(),
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
        var authenticationState = new AuthenticationState(
            journeyId,
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow,
            oAuthState: null,
            firstTimeSignInForEmail: firstTimeSignInForEmail);

        authenticationState.OnSignedInUserProvided(user);

        // Assert
        Assert.Equal(user.DateOfBirth, authenticationState.DateOfBirth);
        Assert.Equal(user.EmailAddress, authenticationState.EmailAddress);
        Assert.True(authenticationState.EmailAddressVerified);
        Assert.Equal(user.FirstName, authenticationState.FirstName);
        Assert.Equal(user.MiddleName, authenticationState.MiddleName);
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
            MiddleName = Faker.Name.First(),
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
        var authenticationState = new AuthenticationState(
            journeyId,
            userRequirements,
            postSignInUrl: "/",
            startedAt: DateTime.UtcNow,
            oAuthState: null,
            firstTimeSignInForEmail: firstTimeSignInForEmail);

        authenticationState.OnSignedInUserProvided(user);

        // Assert
        Assert.Equal(user.DateOfBirth, authenticationState.DateOfBirth);
        Assert.Equal(user.EmailAddress, authenticationState.EmailAddress);
        Assert.True(authenticationState.EmailAddressVerified);
        Assert.Equal(user.FirstName, authenticationState.FirstName);
        Assert.Equal(user.MiddleName, authenticationState.MiddleName);
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
        var middleName = Faker.Name.Middle();
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
            MiddleName = middleName,
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
            new Claim(Claims.Name, firstName + " " + middleName + " " + lastName),
            new Claim(Claims.GivenName, firstName),
            new Claim(Claims.MiddleName, middleName),
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
        var middleName = Faker.Name.Middle();
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
            MiddleName = middleName,
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
            new Claim(Claims.Name, firstName + " " + middleName + " " + lastName),
            new Claim(Claims.GivenName, firstName),
            new Claim(Claims.MiddleName, middleName),
            new Claim(Claims.FamilyName, lastName),
            new Claim(Claims.Role, StaffRoles.GetAnIdentityAdmin),
            new Claim(Claims.Role, StaffRoles.GetAnIdentitySupport),
            new Claim(CustomClaims.UserType, userType.ToString())
        };
        Assert.Equal(expectedClaims.OrderBy(c => c.Type), claims.OrderBy(c => c.Type), new ClaimTypeAndValueEqualityComparer());
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
        var middleName = Faker.Name.Middle();
        var lastName = Faker.Name.Last();
        var userId = Guid.NewGuid();

        var user = new User()
        {
            DateOfBirth = dateOfBirth,
            Created = DateTime.UtcNow,
            EmailAddress = email,
            FirstName = firstName,
            MiddleName = middleName,
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
        Assert.Equal(middleName, authenticationState.MiddleName);
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
        var middleName = Faker.Name.Middle();
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
            MiddleName = middleName,
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
        Assert.Equal(middleName, authenticationState.MiddleName);
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
        var middleName = Faker.Name.Middle();
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
            MiddleName = middleName,
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
        Assert.Equal(middleName, authenticationState.MiddleName);
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
        var middleName = Faker.Name.Middle();
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
            MiddleName = middleName,
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
        Assert.Equal(middleName, authenticationState.MiddleName);
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
        var middleName = Faker.Name.Middle();
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
            MiddleName = middleName,
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
        Assert.Equal(middleName, authenticationState.MiddleName);
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
}
