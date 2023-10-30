using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.UserVerification;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

[Collection(nameof(DisableParallelization))]
public class EmailConfirmationTests : TestBase
{
    public EmailConfirmationTests(HostFixture hostFixture)
        : base(hostFixture)
    {
        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinVerification(TestRequestClientIpProvider.ClientIpAddress)).ReturnsAsync(false);
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/email-confirmation");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/email-confirmation");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/email-confirmation");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.EmailSet(), additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/email-confirmation");
    }

    [Fact]
    public async Task Get_EmailNotKnown_RedirectsToEmailPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/email", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(authStateHelper.AuthenticationState.EmailAddress, doc.GetElementByTestId("email")?.TextContent);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/email-confirmation");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/email-confirmation");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/email-confirmation");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.EmailSet(), additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/email-confirmation");
    }

    [Fact]
    public async Task Post_EmailNotKnown_RedirectsToEmailPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/email", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_UnknownPin_ReturnsError()
    {
        // Arrange
        var email = Faker.Internet.Email();

        // The real PIN generation service never generates pins that start with a '0'
        var pin = "01234";

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(email), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
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
        var pin = "0";

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(email), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
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
        var pin = "0123345678";

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(email), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
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
        var pin = "abc";

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(email), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
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

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(email);
        Clock.AdvanceBy(TimeSpan.FromHours(1));
        SpyRegistry.Get<IUserVerificationService>().Reset();

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(email), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
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

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateEmailPin(email), Times.Once);
    }

    [Fact]
    public async Task Post_PinExpiredMoreThanTwoHoursAgo_ReturnsErrorAndDoesNotSendANewPin()
    {
        // Arrange
        var email = Faker.Internet.Email();

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var userVerificationOptions = HostFixture.Services.GetRequiredService<IOptions<UserVerificationOptions>>();
        var pinResult = await userVerificationService.GenerateEmailPin(email);
        Clock.AdvanceBy(TimeSpan.FromHours(2) + TimeSpan.FromSeconds(userVerificationOptions.Value.PinLifetimeSeconds));
        SpyRegistry.Get<IUserVerificationService>().Reset();

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(email), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
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

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateEmailPin(email), Times.Never);
    }

    [Theory]
    [InlineData(null, null, "/sign-in/register/no-account")]
    [InlineData(CustomScopes.DqtRead, TrnRequirementType.Optional, "/sign-in/register/no-account")]
    public async Task Post_ValidPinForNewUser_UpdatesAuthenticationStateAndRedirects(string? scope, TrnRequirementType? trnRequirementType, string expectedRedirectLocation)
    {
        // Arrange
        var email = Faker.Internet.Email();

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(email);

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(email), scope, trnRequirementType);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
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
        Assert.StartsWith(expectedRedirectLocation, response.Headers.Location?.OriginalString);

        Assert.True(authStateHelper.AuthenticationState.EmailAddressVerified);
        Assert.True(authStateHelper.AuthenticationState.FirstTimeSignInForEmail);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ValidPinForKnownUserWithTrn_UpdatesAuthenticationStateSignsInAndRedirects(bool hasTrn)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: hasTrn);

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(user.EmailAddress);

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(user.EmailAddress), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
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
        Assert.Equal(authStateHelper.AuthenticationState.PostSignInUrl, response.Headers.Location?.OriginalString);

        Assert.True(authStateHelper.AuthenticationState.EmailAddressVerified);
        Assert.NotNull(authStateHelper.AuthenticationState.UserId);
        Assert.False(authStateHelper.AuthenticationState.FirstTimeSignInForEmail);
        Assert.Equal(user.Trn, authStateHelper.AuthenticationState.Trn);
    }

    [Fact]
    public async Task Post_ValidPinForAdminScopeWithUnknownUser_ReturnsForbidden()
    {
        // Arrange
        var email = Faker.Internet.Email();

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(email);

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(email), CustomScopes.UserRead);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidPinForAdminScopeWithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(user.EmailAddress);

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(user.EmailAddress), CustomScopes.UserRead);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidPinForAdminScopeForKnownUserWithTrn_UpdatesAuthenticationStateSignsInAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Staff);
        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(user.EmailAddress);

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(user.EmailAddress), CustomScopes.UserRead);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
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
        Assert.Equal(authStateHelper.AuthenticationState.PostSignInUrl, response.Headers.Location?.OriginalString);

        Assert.True(authStateHelper.AuthenticationState.EmailAddressVerified);
        Assert.NotNull(authStateHelper.AuthenticationState.UserId);
        Assert.False(authStateHelper.AuthenticationState.FirstTimeSignInForEmail);
    }

    [Fact]
    public async Task Post_BlockedClient_ReturnsTooManyRequestsStatusCode()
    {
        // Arrange
        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinVerification(TestRequestClientIpProvider.ClientIpAddress)).ReturnsAsync(true);

        var email = Faker.Internet.Email();
        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(email);

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(email), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidPinForNonAdminScopeWithAdminUser_ReturnsForbidden()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Staff);

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(user.EmailAddress);

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.EmailSet(user.EmailAddress),
            additionalScopes: CustomScopes.DefaultUserTypesScopes.First());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidPinForKnownUserWithTrnAndDifferentDqtName_UpdatesUserSignsInAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(user.EmailAddress);

        var dqtFirstName = Faker.Name.First();
        var dqtMiddleName = Faker.Name.Middle();
        var dqtLastName = Faker.Name.Last();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthServer.Services.DqtApi.TeacherInfo()
            {
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                Email = Faker.Internet.Email(),
                FirstName = dqtFirstName,
                MiddleName = dqtMiddleName,
                LastName = dqtLastName,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingDateOfBirthChange = false,
                PendingNameChange = false,
                Alerts = Array.Empty<AlertInfo>()
            });

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(user.EmailAddress), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
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
        Assert.Equal(authStateHelper.AuthenticationState.PostSignInUrl, response.Headers.Location?.OriginalString);

        Assert.True(authStateHelper.AuthenticationState.EmailAddressVerified);
        Assert.NotNull(authStateHelper.AuthenticationState.UserId);
        Assert.False(authStateHelper.AuthenticationState.FirstTimeSignInForEmail);
        Assert.Equal(user.Trn, authStateHelper.AuthenticationState.Trn);
        Assert.Equal(dqtFirstName, authStateHelper.AuthenticationState.FirstName);
        Assert.Equal(dqtMiddleName, authStateHelper.AuthenticationState.MiddleName);
        Assert.Equal(dqtLastName, authStateHelper.AuthenticationState.LastName);

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                Assert.Equal(UserUpdatedEventSource.DqtSynchronization, userUpdatedEvent.Source);
                Assert.Equal(UserUpdatedEventChanges.FirstName | UserUpdatedEventChanges.MiddleName | UserUpdatedEventChanges.LastName, userUpdatedEvent.Changes);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
                Assert.Equal(dqtFirstName, userUpdatedEvent.User.FirstName);
                Assert.Equal(dqtMiddleName, userUpdatedEvent.User.MiddleName);
                Assert.Equal(dqtLastName, userUpdatedEvent.User.LastName);
            });
    }
}
