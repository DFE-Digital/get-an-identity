using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class TrnInUseTests : TestBase
{
    public TrnInUseTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/different-email");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/different-email");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.Trn, trnRequirementType: null, HttpMethod.Get, "/sign-in/trn/different-email");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        await JourneyHasExpired_RendersErrorPage(
            c => c.TrnLookupCompletedForExistingTrn(existingTrnOwner, email),
            CustomScopes.Trn,
            trnRequirementType: null,
            HttpMethod.Get,
            "/sign-in/trn/different-email");
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

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}");

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

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.AuthenticationState.PostSignInUrl, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ExistingTrnOwnerAlreadyVerified_Redirects()
    {
        // Arrange
        var existingUser = await TestData.CreateUser(hasTrn: true);

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookup(AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified, existingUser),
            CustomScopes.Trn,
            trnRequirementType: null);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrn(existingTrnOwner, email),
            CustomScopes.Trn,
            trnRequirementType: null);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(new Redactor().RedactEmail(existingTrnOwner.EmailAddress), doc.GetElementByTestId("Email")?.TextContent);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/different-email");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/different-email");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.Trn, trnRequirementType: null, HttpMethod.Post, "/sign-in/trn/different-email");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        await JourneyHasExpired_RendersErrorPage(
            c => c.TrnLookupCompletedForExistingTrn(existingTrnOwner, email),
            CustomScopes.Trn,
            trnRequirementType: null,
            HttpMethod.Post,
            "/sign-in/trn/different-email");
    }

    [Theory]
    [InlineData(TrnRequirementType.Optional, "/sign-in/register/")]
    [InlineData(TrnRequirementType.Required, "/sign-in/register/")]
    public async Task Post_TrnNotFound_Redirects(TrnRequirementType trnRequirementType, string expectedRedirectLocation)
    {
        // Arrange
        var pin = "12345";

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookup(AuthenticationState.TrnLookupState.None, user: null),
            CustomScopes.Trn,
            trnRequirementType);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
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
        var user = await TestData.CreateUser(hasTrn: true);

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pin = await userVerificationService.GenerateEmailPin(user.EmailAddress);

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookup(AuthenticationState.TrnLookupState.Complete, user),
            CustomScopes.Trn,
            trnRequirementType: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.AuthenticationState.PostSignInUrl, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_ExistingTrnOwnerAlreadyVerified_Redirects()
    {
        // Arrange
        var existingUser = await TestData.CreateUser(hasTrn: true);

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pin = await userVerificationService.GenerateEmailPin(existingUser.EmailAddress);

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookup(AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified, existingUser),
            CustomScopes.Trn,
            trnRequirementType: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_UnknownPin_ReturnsError()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        // The real PIN generation service never generates pins that start with a '0'
        var pin = "01234";

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrn(existingTrnOwner, email),
            CustomScopes.Trn,
            trnRequirementType: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "Enter a correct security code");
    }

    [Fact]
    public async Task Post_PinTooShort_ReturnsError()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var pin = "0";

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrn(existingTrnOwner, email),
            CustomScopes.Trn,
            trnRequirementType: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "You’ve not entered enough numbers, the code must be 5 numbers");
    }

    [Fact]
    public async Task Post_PinTooLong_ReturnsError()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var pin = "0123345678";

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrn(existingTrnOwner, email),
            CustomScopes.Trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "You’ve entered too many numbers, the code must be 5 numbers");
    }

    [Fact]
    public async Task Post_NonNumericPin_ReturnsError()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var pin = "abc";

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrn(existingTrnOwner, email),
            CustomScopes.Trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "The code must be 5 numbers");
    }

    [Fact]
    public async Task Post_PinExpiredLessThanTwoHoursAgo_ReturnsErrorAndSendsANewPin()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(existingTrnOwner.EmailAddress);
        Clock.AdvanceBy(TimeSpan.FromHours(1));
        SpyRegistry.Get<IUserVerificationService>().Reset();

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrn(existingTrnOwner, email),
            CustomScopes.Trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "The security code has expired. New code sent.");

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateEmailPin(existingTrnOwner.EmailAddress), Times.Once);
    }

    [Fact]
    public async Task Post_PinExpiredMoreThanTwoHoursAgo_ReturnsErrorAndDoesNotSendANewPin()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var userVerificationOptions = HostFixture.Services.GetRequiredService<IOptions<UserVerificationOptions>>();
        var pinResult = await userVerificationService.GenerateEmailPin(existingTrnOwner.EmailAddress);
        Clock.AdvanceBy(TimeSpan.FromHours(2) + TimeSpan.FromSeconds(userVerificationOptions.Value.PinLifetimeSeconds));
        SpyRegistry.Get<IUserVerificationService>().Reset();

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrn(existingTrnOwner, email),
            CustomScopes.Trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "Enter a correct security code");

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateEmailPin(existingTrnOwner.EmailAddress), Times.Never);
    }

    [Fact]
    public async Task Post_ValidPin_UpdatesAuthenticationStateAndRedirects()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(existingTrnOwner.EmailAddress);

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.TrnLookupCompletedForExistingTrn(existingTrnOwner, email),
            CustomScopes.Trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/choose-email", response.Headers.Location?.OriginalString);

        Assert.Equal(AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified, authStateHelper.AuthenticationState.TrnLookup);
    }
}
