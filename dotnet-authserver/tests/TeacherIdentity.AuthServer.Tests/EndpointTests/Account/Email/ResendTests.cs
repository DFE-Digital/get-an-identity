using System.Text.Encodings.Web;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.Email;

public class ResendTests : TestBase
{
    private readonly ProtectedString _email;

    public ResendTests(HostFixture hostFixture)
        : base(hostFixture)
    {
        _email = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(Faker.Internet.Email());
    }

    [Fact]
    public async Task Post_EmptyEmail_ReturnsError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/email/resend?email={_email}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "NewEmail", "Enter your new email address");
    }

    [Theory]
    [MemberData(nameof(InvalidEmailData))]
    public async Task Post_InvalidEmail_RendersError(string newEmail, string expectedErrorMessage)
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/email/resend?email={_email}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewEmail", newEmail }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "NewEmail", expectedErrorMessage);
    }

    [Fact]
    public async Task Post_EmailInUse_RendersError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var anotherUser = await TestData.CreateUser();
        var newEmail = anotherUser.EmailAddress;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/email/resend?email={_email}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewEmail", newEmail }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "NewEmail", "This email address is already in use - Enter a different email address");
    }

    [Fact]
    public async Task Post_EmailMatchesCurrentEmail_DoesNotProduceError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/email/resend?email={_email}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewEmail", user.EmailAddress }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.True((int)response.StatusCode < 400);
    }

    [Fact]
    public async Task Post_ValidRequest_GeneratesPinAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var newEmail = Faker.Internet.Email();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/email/resend?email={_email}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewEmail", newEmail }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/account/email/confirm", response.Headers.Location?.OriginalString);

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateEmailPin(newEmail), Times.Once);
    }

    [Fact]
    public async Task Post_ValidRequest_RedirectsWithCorrectReturnUrl()
    {
        // Arrange
        var client = TestClients.Client1;
        var redirectUri = client.RedirectUris.First().GetLeftPart(UriPartial.Authority);

        var returnUrl = UrlEncoder.Default.Encode($"/account?client_id={client.ClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/email/resend?email={_email}&returnUrl={returnUrl}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewEmail", Faker.Internet.Email() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Contains($"returnUrl={returnUrl}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_PinGenerationRateLimitedExceeded_ReturnsTooManyRequests()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        HostFixture.RateLimitStore
            .Setup(x => x.IsClientIpBlockedForPinGeneration(TestRequestClientIpProvider.ClientIpAddress))
            .ReturnsAsync(true);

        var newEmail = Faker.Internet.Email();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/email/resend?email={_email}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewEmail", newEmail }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, (int)response.StatusCode);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("academy")]
    public async Task Post_EmailWithInvalidPrefix_ReturnsError(string emailPrefix)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/email/resend?email={_email}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewEmail", TestData.GenerateUniqueEmail(emailPrefix) }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "NewEmail", "Enter a personal email address not one from a work or education setting.");
    }

    [Fact]
    public async Task Post_EmailWithInvalidSuffix_ReturnsError()
    {
        // Arrange
        var invalidSuffix = "myschool1231.sch.uk";

        await TestData.WithDbContext(async dbContext =>
        {
            var establishmentDomain = new EstablishmentDomain
            {
                DomainName = invalidSuffix
            };

            dbContext.EstablishmentDomains.Add(establishmentDomain);
            await dbContext.SaveChangesAsync();
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/email/resend?email={_email}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewEmail", TestData.GenerateUniqueEmail(suffix: invalidSuffix) }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "NewEmail", "Enter a personal email address not one from a work or education setting.");
    }

    public static TheoryData<string, string> InvalidEmailData { get; } = new()
    {
        { "", "Enter your new email address" },
        { "xx", "Enter a valid email address" }
    };
}
