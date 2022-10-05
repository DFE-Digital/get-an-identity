using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
public class TrnInUseTests : TestBase
{
    public TrnInUseTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/different-email");
    }

    [Theory]
    [InlineData(AuthenticationState.TrnLookupState.None)]
    [InlineData(AuthenticationState.TrnLookupState.Complete)]
    [InlineData(AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified)]
    public async Task Get_TrnLookupStateIsInvalid_RedirectsToNextPage(AuthenticationState.TrnLookupState trnLookupState)
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = TestData.GenerateTrn();

        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pin = await emailVerificationService.GeneratePin(existingTrnOwner.EmailAddress);

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
                        Updated = Clock.UtcNow,
                        UserId = Guid.NewGuid(),
                        UserType = UserType.Default
                    },
                    firstTimeSignInForEmail: true);
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

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        var authStateHelper = CreateAuthenticationStateHelper(email, existingTrnOwner.EmailAddress);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(new Redactor().RedactEmail(existingTrnOwner.EmailAddress), doc.GetElementByTestId("Email")?.TextContent);
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/different-email");
    }

    [Theory]
    [InlineData(AuthenticationState.TrnLookupState.None)]
    [InlineData(AuthenticationState.TrnLookupState.Complete)]
    [InlineData(AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified)]
    public async Task Post_TrnLookupStateIsInvalid_RedirectsToNextPage(AuthenticationState.TrnLookupState trnLookupState)
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = TestData.GenerateTrn();

        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pin = await emailVerificationService.GeneratePin(existingTrnOwner.EmailAddress);

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
                        Updated = Clock.UtcNow,
                        UserId = Guid.NewGuid(),
                        UserType = UserType.Default
                    },
                    firstTimeSignInForEmail: true);
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

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pin)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_UnknownPin_ReturnsError()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        // The real PIN generation service never generates pins that start with a '0'
        var pin = "01234";

        var authStateHelper = CreateAuthenticationStateHelper(email, existingTrnOwner.EmailAddress);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pin)
                .ToContent()
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

        var authStateHelper = CreateAuthenticationStateHelper(email, existingTrnOwner.EmailAddress);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pin)
                .ToContent()
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

        var authStateHelper = CreateAuthenticationStateHelper(email, existingTrnOwner.EmailAddress);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pin)
                .ToContent()
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

        var authStateHelper = CreateAuthenticationStateHelper(email, existingTrnOwner.EmailAddress);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pin)
                .ToContent()
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
        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pin = await emailVerificationService.GeneratePin(existingTrnOwner.EmailAddress);
        Clock.AdvanceBy(TimeSpan.FromHours(1));
        Spy.Get(emailVerificationService).Reset();

        var authStateHelper = CreateAuthenticationStateHelper(email, existingTrnOwner.EmailAddress);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pin)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "The security code has expired. New code sent.");

        HostFixture.EmailVerificationService.Verify(mock => mock.GeneratePin(existingTrnOwner.EmailAddress), Times.Once);
    }

    [Fact]
    public async Task Post_PinExpiredMoreThanTwoHoursAgo_ReturnsErrorAndDoesNotSendANewPin()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var emailVerificationOptions = HostFixture.Services.GetRequiredService<IOptions<EmailVerificationOptions>>();
        var pin = await emailVerificationService.GeneratePin(existingTrnOwner.EmailAddress);
        Clock.AdvanceBy(TimeSpan.FromHours(2) + TimeSpan.FromSeconds(emailVerificationOptions.Value.PinLifetimeSeconds));
        Spy.Get(emailVerificationService).Reset();

        var authStateHelper = CreateAuthenticationStateHelper(email, existingTrnOwner.EmailAddress);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pin)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "Enter a correct security code");

        HostFixture.EmailVerificationService.Verify(mock => mock.GeneratePin(existingTrnOwner.EmailAddress), Times.Never);
    }

    [Fact]
    public async Task Post_ValidPin_UpdatesAuthenticationStateAndRedirects()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);
        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pin = await emailVerificationService.GeneratePin(existingTrnOwner.EmailAddress);

        var authStateHelper = CreateAuthenticationStateHelper(email, existingTrnOwner.EmailAddress);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pin)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);

        Assert.Equal(AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified, authStateHelper.AuthenticationState.TrnLookup);
    }

    private AuthenticationStateHelper CreateAuthenticationStateHelper(string email, string existingTrnOwnerEmail) =>
        CreateAuthenticationStateHelper(authState =>
        {
            authState.OnEmailSet(email);
            authState.OnEmailVerified(user: null);
            authState.OnTrnLookupCompletedForTrnAlreadyInUse(existingTrnOwnerEmail);
        });
}
