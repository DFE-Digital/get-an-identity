using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
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
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: CustomScopes.Trn, trnRequirementType: null, HttpMethod.Get, "/sign-in/trn/choose-email");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        await JourneyHasExpired_RendersErrorPage(
            c => c.TrnLookupCompletedForExistingTrnAndOwnerEmailVerified(existingTrnOwner, email),
            CustomScopes.Trn,
            trnRequirementType: null,
            HttpMethod.Get,
            "/sign-in/trn/choose-email");
    }

    [Theory]
    [InlineData(TrnRequirementType.Optional, "/sign-in/register/")]
    [InlineData(TrnRequirementType.Required, "/sign-in/register/")]
    public async Task Get_TrnNotFound_Redirects(TrnRequirementType trnRequirementType, string expectedRedirectLocation)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookup(AuthenticationState.TrnLookupState.None, user: null),
            CustomScopes.Trn,
            trnRequirementType);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/choose-email?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith(expectedRedirectLocation, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_TrnFoundButDidNotConflictWithExistingAccount_Redirects()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookup(AuthenticationState.TrnLookupState.Complete, user),
            CustomScopes.Trn,
            trnRequirementType: null);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/choose-email?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.AuthenticationState.PostSignInUrl, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ExistingTrnOwnerNotVerified_Redirects()
    {
        // Arrange
        var existingUser = await TestData.CreateUser(hasTrn: true);

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookup(AuthenticationState.TrnLookupState.ExistingTrnFound, existingUser),
            CustomScopes.Trn,
            trnRequirementType: null);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/choose-email?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersEnteredAndMatchedEmailAddresses()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrnAndOwnerEmailVerified(existingTrnOwner, email),
            CustomScopes.Trn,
            trnRequirementType: null);

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
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.Trn, trnRequirementType: null, HttpMethod.Post, "/sign-in/trn/choose-email");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        await JourneyHasExpired_RendersErrorPage(
            c => c.TrnLookupCompletedForExistingTrnAndOwnerEmailVerified(existingTrnOwner, email),
            CustomScopes.Trn,
            trnRequirementType: null,
            HttpMethod.Post,
            "/sign-in/trn/choose-email");
    }

    [Theory]
    [InlineData(TrnRequirementType.Optional, "/sign-in/register/")]
    [InlineData(TrnRequirementType.Required, "/sign-in/register/")]
    public async Task Post_TrnNotFound_Redirects(TrnRequirementType trnRequirementType, string expectedRedirectLocation)
    {
        // Arrange
        var email = Faker.Internet.Email();

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookup(AuthenticationState.TrnLookupState.None, user: null),
            CustomScopes.Trn,
            trnRequirementType);

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
        Assert.StartsWith(expectedRedirectLocation, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_TrnFoundButDidNotConflictWithExistingAccount_Redirects()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var user = await TestData.CreateUser(hasTrn: true);

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookup(AuthenticationState.TrnLookupState.Complete, user),
            CustomScopes.Trn,
            trnRequirementType: null);

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
        Assert.Equal(authStateHelper.AuthenticationState.PostSignInUrl, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_ExistingTrnOwnerNotVerified_Redirects()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingUser = await TestData.CreateUser(hasTrn: true);

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookup(AuthenticationState.TrnLookupState.ExistingTrnFound, existingUser),
            CustomScopes.Trn,
            trnRequirementType: null);

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
        Assert.StartsWith("/sign-in/trn", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_NoEmailChosen_RendersError()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrnAndOwnerEmailVerified(existingTrnOwner, email),
            CustomScopes.Trn,
            trnRequirementType: null);

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
            c => c.TrnLookupCompletedForExistingTrnAndOwnerEmailVerified(existingTrnOwner, email),
            CustomScopes.Trn,
            trnRequirementType: null);

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
    public async Task Post_ValidRequest_UpdatesUserAndRedirectsToNextPage(bool newEmailChosen)
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var chosenEmail = newEmailChosen ? email : existingTrnOwner.EmailAddress;

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrnAndOwnerEmailVerified(existingTrnOwner, email),
            CustomScopes.Trn,
            trnRequirementType: null);

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
        Assert.Equal(authStateHelper.AuthenticationState.PostSignInUrl, response.Headers.Location?.OriginalString);

        Assert.Equal(authStateHelper.AuthenticationState.EmailAddress, chosenEmail);
        Assert.Equal(authStateHelper.AuthenticationState.UserId, existingTrnOwner.UserId);
        Assert.Equal(AuthenticationState.TrnLookupState.Complete, authStateHelper.AuthenticationState.TrnLookup);

        var userId = await TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.Where(u => u.UserId == existingTrnOwner.UserId).SingleAsync();

            Assert.NotNull(user);
            Assert.Equal(chosenEmail, user.EmailAddress);

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
