using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

[Collection(nameof(DisableParallelization))] // relies on mocks
public class TrnCallbackTests : TestBase
{
    public TrnCallbackTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/callback");
    }

    [Theory]
    [InlineData(AuthenticationState.TrnLookupState.Complete)]
    [InlineData(AuthenticationState.TrnLookupState.ExistingTrnFound)]
    [InlineData(AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified)]
    public async Task Get_TrnLookupStateIsInvalid_RedirectsToNextPage(AuthenticationState.TrnLookupState trnLookupState)
    {
        // Arrange
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = TestData.GenerateTrn();

        var authStateHelper = CreateAuthenticationStateHelper(authState =>
        {
            authState.OnEmailSet(email);
            authState.OnEmailVerified(user: null);

            if (trnLookupState == AuthenticationState.TrnLookupState.Complete)
            {
                authState.OnTrnLookupCompletedAndUserRegistered(
                    new User()
                    {
                        CompletedTrnLookup = Clock.UtcNow,
                        Created = Clock.UtcNow,
                        DateOfBirth = dateOfBirth,
                        EmailAddress = email,
                        FirstName = firstName,
                        LastName = lastName,
                        Trn = trn,
                        UserId = Guid.NewGuid(),
                        UserType = UserType.Default
                    },
                    firstTimeSignInForEmail: true);
            }
            else if (trnLookupState == AuthenticationState.TrnLookupState.ExistingTrnFound)
            {
                authState.OnTrnLookupCompletedForTrnAlreadyInUse(existingTrnOwnerEmail: Faker.Internet.Email());
            }
            else if (trnLookupState == AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified)
            {
                authState.OnTrnLookupCompletedForTrnAlreadyInUse(existingTrnOwnerEmail: Faker.Internet.Email());
                authState.OnEmailVerifiedOfExistingAccountForTrn();
            }
            else
            {
                throw new NotImplementedException($"Unknown {nameof(AuthenticationState.TrnLookupState)}: '{trnLookupState}'.");
            }
        });

        await SaveLookupState(authStateHelper.AuthenticationState.JourneyId, firstName, lastName, dateOfBirth, trn);

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
        var authStateHelper = CreateAuthenticationStateHelper(authState => authState.OnEmailSet(Faker.Internet.Email()));

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/callback?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_ValidCallback_CreatesUserLocksLookupStateCallsDqtApiAndRedirectsToNextPage(bool hasTrn)
    {
        // Arrange
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = hasTrn ? TestData.GenerateTrn() : null;

        var authStateHelper = CreateAuthenticationStateHelper(email);

        await SaveLookupState(authStateHelper.AuthenticationState.JourneyId, firstName, lastName, dateOfBirth, trn);

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
            Assert.Equal(UserType.Default, user.UserType);

            var dqtApiCallExpectedTimes = hasTrn ? Times.Once() : Times.Never();
            HostFixture.DqtApiClient.Verify(mock => mock.SetTeacherIdentityInfo(It.Is<DqtTeacherIdentityInfo>(x => x.UserId == user!.UserId && x.Trn == trn)), dqtApiCallExpectedTimes);

            var lookupState = await dbContext.JourneyTrnLookupStates.SingleAsync(s => s.JourneyId == authStateHelper.AuthenticationState.JourneyId);
            Assert.Equal(Clock.UtcNow, lookupState.Locked);

            return user.UserId;
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userSignedInEvent = Assert.IsType<UserSignedIn>(e);
                Assert.Equal(Clock.UtcNow, userSignedInEvent.CreatedUtc);
                Assert.Equal(authStateHelper.AuthenticationState.OAuthState?.ClientId, userSignedInEvent.ClientId);
                Assert.Equal(authStateHelper.AuthenticationState.OAuthState?.Scope, userSignedInEvent.Scope);
                Assert.Equal(userId, userSignedInEvent.UserId);
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

        var authStateHelper = CreateAuthenticationStateHelper(email);

        await SaveLookupState(authStateHelper.AuthenticationState.JourneyId, firstName, lastName, dateOfBirth, trn);

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

    private AuthenticationStateHelper CreateAuthenticationStateHelper(string? email = null) =>
        CreateAuthenticationStateHelper(authState =>
        {
            authState.OnEmailSet(email ?? Faker.Internet.Email());
            authState.OnEmailVerified(user: null);
        });

    private const string GenerateRandomTrnSentinel = "0000000";

    private Task SaveLookupState(
        Guid journeyId,
        string? firstName = null,
        string? lastName = null,
        DateOnly? dateOfBirth = null,
        string? trn = GenerateRandomTrnSentinel)
    {
        if (trn == GenerateRandomTrnSentinel)
        {
            trn = TestData.GenerateTrn();
        }

        return TestData.WithDbContext(async dbContext =>
        {
            dbContext.JourneyTrnLookupStates.Add(new JourneyTrnLookupState()
            {
                JourneyId = journeyId,
                Created = Clock.UtcNow,
                DateOfBirth = dateOfBirth ?? DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = firstName ?? Faker.Name.First(),
                LastName = lastName ?? Faker.Name.Last(),
                Trn = trn,
                NationalInsuranceNumber = null
            });

            await dbContext.SaveChangesAsync();
        });
    }
}
