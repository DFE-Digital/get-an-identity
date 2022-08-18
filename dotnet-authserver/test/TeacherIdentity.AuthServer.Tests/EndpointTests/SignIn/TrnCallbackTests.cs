using Flurl;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class TrnCallbackTests : TestBase
{
    public TrnCallbackTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn-callback");
    }

    [Fact]
    public async Task Get_MissingStateInDb_ReturnsError()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper(authState => authState.EmailAddress = Faker.Internet.Email());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn-callback?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_ValidCallback_CreatesUserLocksLookupStateCallsDqtApiAndRedirectsToConfirmation(bool hasTrn)
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper();

        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = hasTrn ? TestData.GenerateTrn() : null;

        A.CallTo(() => HostFixture.DqtApiClient!.SetTeacherIdentityInfo(A<DqtTeacherIdentityInfo>.That.Matches(i => i.Trn == trn)))
            .Returns(Task.CompletedTask);

        await SaveLookupState(authStateHelper.AuthenticationState.JourneyId, firstName, lastName, dateOfBirth, trn);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn-callback?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal("/connect/authorize", new Url(response.Headers.Location).Path);

        await TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.Where(u => u.FirstName == firstName && u.LastName == lastName && u.DateOfBirth == dateOfBirth).SingleOrDefaultAsync();

            Assert.NotNull(user);
            Assert.Equal(authStateHelper.AuthenticationState.UserId, user!.UserId);
            Assert.Equal(authStateHelper.AuthenticationState.EmailAddress, user!.EmailAddress);
            Assert.Equal(firstName, user!.FirstName);
            Assert.Equal(lastName, user!.LastName);
            Assert.Equal(dateOfBirth, user!.DateOfBirth);

            if (hasTrn)
            {
                A.CallTo(() => HostFixture.DqtApiClient
                    !.SetTeacherIdentityInfo(A<DqtTeacherIdentityInfo>.That.Matches(x => x.UserId == user!.UserId && x.Trn == trn)))
                    .MustHaveHappenedOnceExactly();
            }
            else
            {
                A.CallTo(() => HostFixture.DqtApiClient
                    !.SetTeacherIdentityInfo(A<DqtTeacherIdentityInfo>.That.Matches(x => x.UserId == user!.UserId)))
                    .MustNotHaveHappened();
            }

            var lookupState = await dbContext.JourneyTrnLookupStates.SingleAsync(s => s.JourneyId == authStateHelper.AuthenticationState.JourneyId);
            Assert.Equal(Clock.UtcNow, lookupState.Locked);
        });
    }

    [Fact]
    public async Task Get_LookupStateIsLocked_ReturnsError()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper();

        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());

        await SaveLookupState(authStateHelper.AuthenticationState.JourneyId, firstName, lastName, dateOfBirth, locked: Clock.UtcNow);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn-callback?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidCallbackButApiCallFails_ReturnsError()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper();

        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = TestData.GenerateTrn();
        authStateHelper.AuthenticationState.Trn = trn;

        A.CallTo(() => HostFixture.DqtApiClient!.SetTeacherIdentityInfo(A<DqtTeacherIdentityInfo>._)).Throws(new InvalidOperationException());

        await SaveLookupState(authStateHelper.AuthenticationState.JourneyId, firstName, lastName, dateOfBirth, trn);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn-callback?{authStateHelper.ToQueryParam()}");

        // Act
        var ex = await Record.ExceptionAsync(() => HttpClient.SendAsync(request));

        // Assert
        Assert.NotNull(ex);
    }

    private AuthenticationStateHelper CreateAuthenticationStateHelper() =>
        CreateAuthenticationStateHelper(authState =>
        {
            authState.EmailAddress = Faker.Internet.Email();
            authState.EmailAddressVerified = true;
        });

    private const string GenerateRandomTrnSentinel = "0000000";

    private Task SaveLookupState(
        Guid journeyId,
        string? firstName = null,
        string? lastName = null,
        DateOnly? dateOfBirth = null,
        string? trn = GenerateRandomTrnSentinel,
        DateTime? locked = null)
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
                Locked = locked
            });

            await dbContext.SaveChangesAsync();
        });
    }
}
