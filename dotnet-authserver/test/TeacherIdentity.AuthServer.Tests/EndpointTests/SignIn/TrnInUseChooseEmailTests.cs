using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class TrnInUseChooseEmailTests : TestBase
{
    public TrnInUseChooseEmailTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/choose-email");
    }

    [Theory]
    [InlineData(AuthenticationState.TrnLookupState.None)]
    [InlineData(AuthenticationState.TrnLookupState.Complete)]
    [InlineData(AuthenticationState.TrnLookupState.ExistingTrnFound)]
    public async Task Get_TrnLookupStateIsInvalid_RedirectsToNextPage(AuthenticationState.TrnLookupState trnLookupState)
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = TestData.GenerateTrn();

        var authStateHelper = CreateAuthenticationStateHelper(authState =>
        {
            authState.OnEmailSet(email);
            authState.OnEmailVerified(user: null);

            if (trnLookupState == AuthenticationState.TrnLookupState.None)
            {
            }
            else if (trnLookupState == AuthenticationState.TrnLookupState.Complete)
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
            else
            {
                throw new NotImplementedException($"Unknown {nameof(AuthenticationState.TrnLookupState)}: '{trnLookupState}'.");
            }
        });

        await SaveLookupState(authStateHelper.AuthenticationState.JourneyId, existingTrnOwner.FirstName, existingTrnOwner.LastName, existingTrnOwner.DateOfBirth, trn);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/choose-email?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersEnteredAndMatchedEmailAddresses()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var trn = existingTrnOwner.Trn;

        var authStateHelper = CreateAuthenticationStateHelper(email, existingTrnOwner.EmailAddress);

        await SaveLookupState(authStateHelper.AuthenticationState.JourneyId, existingTrnOwner.FirstName, existingTrnOwner.LastName, existingTrnOwner.DateOfBirth, trn);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/choose-email?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        var doc = await response.GetDocument();
        Assert.Equal(email, doc.GetElementByTestId("Email-SignedIn")?.TextContent);
        Assert.Equal(existingTrnOwner.EmailAddress, doc.GetElementByTestId("Email-ExistingAccount")?.TextContent);
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/choose-email");
    }

    [Theory]
    [InlineData(AuthenticationState.TrnLookupState.None)]
    [InlineData(AuthenticationState.TrnLookupState.Complete)]
    [InlineData(AuthenticationState.TrnLookupState.ExistingTrnFound)]
    public async Task Post_TrnLookupStateIsInvalid_RedirectsToNextPage(AuthenticationState.TrnLookupState trnLookupState)
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = TestData.GenerateTrn();

        var authStateHelper = CreateAuthenticationStateHelper(authState =>
        {
            authState.OnEmailSet(email);
            authState.OnEmailVerified(user: null);

            if (trnLookupState == AuthenticationState.TrnLookupState.None)
            {
            }
            else if (trnLookupState == AuthenticationState.TrnLookupState.Complete)
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
            else
            {
                throw new NotImplementedException($"Unknown {nameof(AuthenticationState.TrnLookupState)}: '{trnLookupState}'.");
            }
        });

        await SaveLookupState(authStateHelper.AuthenticationState.JourneyId, existingTrnOwner.FirstName, existingTrnOwner.LastName, existingTrnOwner.DateOfBirth, trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/choose-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Email", email)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_NoEmailChosen_RendersError()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var trn = existingTrnOwner.Trn;

        var authStateHelper = CreateAuthenticationStateHelper(email, existingTrnOwner.EmailAddress);

        await SaveLookupState(authStateHelper.AuthenticationState.JourneyId, existingTrnOwner.FirstName, existingTrnOwner.LastName, existingTrnOwner.DateOfBirth, trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/choose-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.ResponseHasError(response, "Email", "Enter the email address you want to use");
    }

    [Fact]
    public async Task Post_SubmittedEmailDoesNotMatchEnteredOrMatchedEmail_RerendersPage()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var trn = existingTrnOwner.Trn;
        var submittedEmail = Faker.Internet.Email();

        var authStateHelper = CreateAuthenticationStateHelper(email, existingTrnOwner.EmailAddress);

        await SaveLookupState(authStateHelper.AuthenticationState.JourneyId, existingTrnOwner.FirstName, existingTrnOwner.LastName, existingTrnOwner.DateOfBirth, trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/choose-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Email", submittedEmail)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Post_ValidRequest_UpdatesUserLocksLookupStateAndRedirectsToNextPage(bool newEmailChosen)
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var trn = existingTrnOwner.Trn;
        var chosenEmail = newEmailChosen ? email : existingTrnOwner.EmailAddress;

        var authStateHelper = CreateAuthenticationStateHelper(email, existingTrnOwner.EmailAddress);

        await SaveLookupState(authStateHelper.AuthenticationState.JourneyId, existingTrnOwner.FirstName, existingTrnOwner.LastName, existingTrnOwner.DateOfBirth, trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/choose-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Email", chosenEmail)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);

        Assert.Equal(authStateHelper.AuthenticationState.EmailAddress, chosenEmail);
        Assert.Equal(authStateHelper.AuthenticationState.UserId, existingTrnOwner.UserId);
        Assert.Equal(AuthenticationState.TrnLookupState.Complete, authStateHelper.AuthenticationState.TrnLookup);

        var userId = await TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.Where(u => u.UserId == existingTrnOwner.UserId).SingleAsync();

            Assert.NotNull(user);
            Assert.Equal(chosenEmail, user.EmailAddress);

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

    private AuthenticationStateHelper CreateAuthenticationStateHelper(string email, string existingTrnOwnerEmail) =>
        CreateAuthenticationStateHelper(authState =>
        {
            authState.OnEmailSet(email);
            authState.OnEmailVerified(user: null);
            authState.OnTrnLookupCompletedForTrnAlreadyInUse(existingTrnOwnerEmail);
            authState.OnEmailVerifiedOfExistingAccountForTrn();
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
                Trn = trn
            });

            await dbContext.SaveChangesAsync();
        });
    }
}
