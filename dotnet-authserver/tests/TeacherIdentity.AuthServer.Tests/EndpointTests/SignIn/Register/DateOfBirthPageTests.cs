using Microsoft.EntityFrameworkCore;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class DateOfBirthPageTests : TestBase
{
    public DateOfBirthPageTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/register/date-of-birth");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/register/date-of-birth");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Get, "/sign-in/register/date-of-birth");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, additionalScopes: null, HttpMethod.Get, "/sign-in/register/date-of-birth");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent(ConfigureValidAuthenticationState, "/sign-in/register/date-of-birth", additionalScopes: null);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/register/date-of-birth");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/register/date-of-birth");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Post, "/sign-in/register/date-of-birth");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, additionalScopes: null, HttpMethod.Post, "/sign-in/register/date-of-birth");
    }

    [Fact]
    public async Task Post_NullDateOfBirth_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/date-of-birth?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "DateOfBirth", "Enter your date of birth");
    }

    [Fact]
    public async Task Post_FutureDateOfBirth_ReturnsError()
    {
        // Arrange
        var dateOfBirth = new DateOnly(2100, 1, 1);

        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/date-of-birth?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "DateOfBirth.Day", dateOfBirth.Day.ToString() },
                { "DateOfBirth.Month", dateOfBirth.Month.ToString() },
                { "DateOfBirth.Year", dateOfBirth.Year.ToString() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "DateOfBirth", "Your date of birth must be in the past");
    }

    [Fact]
    public async Task Post_ValidForm_SetsDateOfBirthOnAuthenticationStateAndRedirects()
    {
        // Arrange
        var dateOfBirth = new DateOnly(2000, 1, 1);

        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/date-of-birth?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "DateOfBirth.Day", dateOfBirth.Day.ToString() },
                { "DateOfBirth.Month", dateOfBirth.Month.ToString() },
                { "DateOfBirth.Year", dateOfBirth.Year.ToString() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/complete", response.Headers.Location?.OriginalString);

        Assert.Equal(dateOfBirth, authStateHelper.AuthenticationState.DateOfBirth);
    }

    [Theory]
    [InlineData("+447123456 789", "07123456789")]
    [InlineData("(0044)7111456789", "07111456789")]
    [InlineData("+672-7-123-456-789", "+6727123456789")]
    [InlineData("00672/7-123-456-789", "006727123456789")]
    public async Task Post_ValidForm_CreatesUserWithCorrectFormatMobileNumber(string mobileNumber, string formattedMobileNumber)
    {
        // Arrange
        var dateOfBirth = new DateOnly(2000, 1, 1);

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.RegisterNameSet(mobileNumber: mobileNumber), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/date-of-birth?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "DateOfBirth.Day", dateOfBirth.Day.ToString() },
                { "DateOfBirth.Month", dateOfBirth.Month.ToString() },
                { "DateOfBirth.Year", dateOfBirth.Year.ToString() },
            }
        };

        // Act
        await HttpClient.SendAsync(request);

        // Assert
        await TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.Where(u => u.EmailAddress == authStateHelper.AuthenticationState.EmailAddress).SingleOrDefaultAsync();
            Assert.NotNull(user);
            Assert.Equal(formattedMobileNumber, user.MobileNumber);
        });
    }


    private Func<AuthenticationState, Task> ConfigureValidAuthenticationState(AuthenticationStateHelper.Configure configure) =>
        configure.RegisterNameSet();
}
