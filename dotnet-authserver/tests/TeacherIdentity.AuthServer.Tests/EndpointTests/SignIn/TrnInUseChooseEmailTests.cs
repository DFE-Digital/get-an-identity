using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class TrnInUseChooseEmailTests : TestBase
{
    public TrnInUseChooseEmailTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/choose-email");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/choose-email");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: CustomScopes.DqtRead, HttpMethod.Get, "/sign-in/trn/choose-email");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        await JourneyHasExpired_RendersErrorPage(
            c => c.TrnLookupCompletedForExistingTrnAndOwnerEmailVerified(email, existingTrnOwner),
            CustomScopes.DqtRead,
            HttpMethod.Get,
            "/sign-in/trn/choose-email");
    }

    [Theory]
    [InlineData(AuthenticationState.TrnLookupState.None)]
    [InlineData(AuthenticationState.TrnLookupState.Complete)]
    [InlineData(AuthenticationState.TrnLookupState.ExistingTrnFound)]
    public async Task Get_TrnLookupStateIsInvalid_RedirectsToNextPage(AuthenticationState.TrnLookupState trnLookupState)
    {
        // Arrange
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookup(trnLookupState, existingTrnOwner),
            CustomScopes.DqtRead);

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

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrnAndOwnerEmailVerified(email, existingTrnOwner),
            CustomScopes.DqtRead);

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
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/choose-email");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/choose-email");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, HttpMethod.Post, "/sign-in/trn/choose-email");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        await JourneyHasExpired_RendersErrorPage(
            c => c.TrnLookupCompletedForExistingTrnAndOwnerEmailVerified(email, existingTrnOwner),
            CustomScopes.DqtRead,
            HttpMethod.Post,
            "/sign-in/trn/choose-email");
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

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookup(trnLookupState, existingTrnOwner),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/choose-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", email }
            }
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

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrnAndOwnerEmailVerified(email, existingTrnOwner),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/choose-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Email", "Enter the email address you want to use");
    }

    [Fact]
    public async Task Post_SubmittedEmailDoesNotMatchEnteredOrMatchedEmail_RerendersPage()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var submittedEmail = Faker.Internet.Email();

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrnAndOwnerEmailVerified(email, existingTrnOwner),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/choose-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", submittedEmail }
            }
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
        var chosenEmail = newEmailChosen ? email : existingTrnOwner.EmailAddress;

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrnAndOwnerEmailVerified(email, existingTrnOwner),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/choose-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", chosenEmail }
            }
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

        // Should get a UserUpdatedEvent if the email address was changed
        if (newEmailChosen)
        {
            EventObserver.AssertEventsSaved(
                e =>
                {
                    var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                    Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                    Assert.Equal(UserUpdatedEventSource.TrnMatchedToExistingUser, userUpdatedEvent.Source);
                    Assert.Equal(userId, userUpdatedEvent.User.UserId);
                });
        }
        else
        {
            EventObserver.AssertEventsSaved();
        }
    }
}
