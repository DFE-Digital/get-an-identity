using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

[Collection(nameof(DisableParallelization))] // relies on mocks
public class TrnCallbackTests : TestBase
{
    public TrnCallbackTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/callback");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/callback");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, HttpMethod.Get, "/sign-in/trn/callback");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = TestData.GenerateTrn();

        await JourneyHasExpired_RendersErrorPage(
            c => c.TrnLookupCallbackCompleted(email, trn, dateOfBirth, firstName, lastName),
            CustomScopes.DqtRead,
            HttpMethod.Get,
            "/sign-in/trn/callback");
    }

    [Theory]
    [InlineData(AuthenticationState.TrnLookupState.Complete)]
    [InlineData(AuthenticationState.TrnLookupState.ExistingTrnFound)]
    [InlineData(AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified)]
    public async Task Get_TrnLookupStateIsInvalid_RedirectsToNextPage(AuthenticationState.TrnLookupState trnLookupState)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.TrnLookup(trnLookupState, user), CustomScopes.DqtRead);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/callback?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_MissingStateInDb_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/callback?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidCallbackWithPreferredNames_CreatesUserWithPreferredNames()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var preferredFirstName = Faker.Name.First();
        var preferredLastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = TestData.GenerateTrn();

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCallbackCompleted(
                email, trn, dateOfBirth, firstName, lastName, preferredFirstName: preferredFirstName, preferredLastName: preferredLastName),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/callback?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.Where(u => u.FirstName == preferredFirstName && u.LastName == preferredLastName && u.DateOfBirth == dateOfBirth).SingleOrDefaultAsync();

            Assert.NotNull(user);
            Assert.Equal(preferredFirstName, user.FirstName);
            Assert.Equal(preferredLastName, user.LastName);
        });
    }

    [Theory]
    [InlineData(true, false, TrnLookupStatus.Found)]
    [InlineData(false, false, TrnLookupStatus.None)]
    [InlineData(false, true, TrnLookupStatus.Pending)]
    public async Task Get_ValidCallback_CreatesUserLocksLookupStateAndRedirectsToNextPage(
        bool hasTrn,
        bool supportTicketCreated,
        TrnLookupStatus expectedTrnLookupStatus)
    {
        // Arrange
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = hasTrn ? TestData.GenerateTrn() : null;

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCallbackCompleted(email, trn, dateOfBirth, firstName, lastName, supportTicketCreated),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/callback?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);

        var userId = await TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.Where(u => u.FirstName == firstName && u.LastName == lastName && u.DateOfBirth == dateOfBirth).SingleOrDefaultAsync();

            Assert.NotNull(user);
            Assert.Equal(authStateHelper.AuthenticationState.UserId, user!.UserId);
            Assert.Equal(authStateHelper.AuthenticationState.EmailAddress, user.EmailAddress);
            Assert.Equal(firstName, user.FirstName);
            Assert.Equal(lastName, user.LastName);
            Assert.Equal(dateOfBirth, user.DateOfBirth);
            Assert.Equal(Clock.UtcNow, user.Created);
            Assert.Equal(Clock.UtcNow, user.CompletedTrnLookup);
            Assert.Equal(Clock.UtcNow, user.Updated);
            Assert.Equal(Clock.UtcNow, user.LastSignedIn);
            Assert.Equal(UserType.Default, user.UserType);
            Assert.Equal(trn, user.Trn);
            Assert.Equal(authStateHelper.AuthenticationState.OAuthState?.ClientId, user.RegisteredWithClientId);
            Assert.Equal(expectedTrnLookupStatus, user.TrnLookupStatus);

            if (hasTrn)
            {
                Assert.Equal(TrnAssociationSource.Lookup, user.TrnAssociationSource);
            }
            else
            {
                Assert.Null(user.TrnAssociationSource);
            }

            var lookupState = await dbContext.JourneyTrnLookupStates.SingleAsync(s => s.JourneyId == authStateHelper.AuthenticationState.JourneyId);
            Assert.Equal(Clock.UtcNow, lookupState.Locked);

            return user.UserId;
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userRegisteredEvent = Assert.IsType<UserRegisteredEvent>(e);
                Assert.Equal(Clock.UtcNow, userRegisteredEvent.CreatedUtc);
                Assert.Equal(authStateHelper.AuthenticationState.OAuthState!.ClientId, userRegisteredEvent.ClientId);
                Assert.Equal(userId, userRegisteredEvent.User.UserId);
            });
    }

    [Fact]
    public async Task ValidCallback_TrnIsAllocatedToAnExistingUser_UpdatesStateGeneratesPinForExistingAccountAndRedirects()
    {
        // Arrange
        var existingUserWithTrn = await TestData.CreateUser(hasTrn: true);

        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = existingUserWithTrn.Trn;

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCallbackCompleted(email, trn, dateOfBirth, firstName, lastName),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/callback?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);

        Assert.Equal(AuthenticationState.TrnLookupState.ExistingTrnFound, authStateHelper.AuthenticationState.TrnLookup);

        // Should not have created a new account
        await TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.Where(u => u.EmailAddress == email).SingleOrDefaultAsync();
            Assert.Null(user);
        });

        HostFixture.EmailVerificationService.Verify(mock => mock.GeneratePin(existingUserWithTrn.EmailAddress));
    }
}
