using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Services.TrnLookup;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

[Collection(nameof(DisableParallelization))]  // Modifies options
public class TrnTests : TestBase
{
    public TrnTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod.Get, "/sign-in/trn");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.EmailVerified(), HttpMethod.Get, "/sign-in/trn");
    }

    [Fact]
    public async Task Get_NoEmail_RedirectsToEmailPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/email", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_EmailNotVerified_RedirectsToEmailConfirmationPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/email-confirmation", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsOk()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenUseNewTrnLookupJourneyTrue_SubmitsToTrnHasTrn()
    {
        // Arrange
        var trnConfig = HostFixture.Services.GetRequiredService<IOptions<FindALostTrnIntegrationOptions>>().Value;
        trnConfig.UseNewTrnLookupJourney = true;

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        var form = doc.GetElementsByTagName("form").Single();
        Assert.StartsWith("/sign-in/trn/has-trn", form.GetAttribute("action"));
        Assert.Equal("GET", form.GetAttribute("method"));
    }
}
