using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
public class IitProviderTests : TestBase
{
    public IitProviderTests(HostFixture hostFixture)
        : base(hostFixture)
    {
        HostFixture.DqtApiClient.Setup(mock => mock.GetIttProviders(It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetIttProvidersResponse?)new GetIttProvidersResponse()
            {
                IttProviders = new IttProvider[]
                {
                    new() { ProviderName = "provider 1" },
                    new() { ProviderName = "provider 2" },
                }
            });
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {

        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/itt-provider");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/itt-provider");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod.Get, "/sign-in/trn/itt-provider");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.OfficialNameSet(), HttpMethod.Get, "/sign-in/trn/itt-provider");
    }

    [Fact]
    public async Task Get_NoEmail_RedirectsToEmailPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/itt-provider?{authStateHelper.ToQueryParam()}");

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

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/itt-provider?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/email-confirmation", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_OfficialNameNotSet_RedirectsToNextHopUrl()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/itt-provider?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_CallsDqtApiRendersContent()
    {
        await ValidRequest_RendersContent("/sign-in/trn/itt-provider", c => c.OfficialNameSet());

        HostFixture.DqtApiClient.Verify(mock => mock.GetIttProviders(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_NullIttProvidersResponse_RendersContentSuccessfully()
    {
        HostFixture.DqtApiClient.Setup(mock => mock.GetIttProviders(It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetIttProvidersResponse?)null);

        await ValidRequest_RendersContent("/sign-in/trn/itt-provider", c => c.OfficialNameSet());
    }

    [Fact]
    public async Task Post_NullHasIttProvider_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/itt-provider?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "HasIttProvider", "Tell us how you were awarded qualified teacher status (QTS)");
    }

    [Fact]
    public async Task Post_EmptyIttProviderName_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/itt-provider?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasIttProvider", true },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "IttProviderName", "Enter your university, SCITT, school or other training provider");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ValidForm_UpdatesAuthenticationState(bool hasIttProvider)
    {
        // Arrange
        var ittProviderName = "provider";

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/itt-provider?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasIttProvider", hasIttProvider },
                { "IttProviderName", ittProviderName },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);

        Assert.Equal(hasIttProvider, authStateHelper.AuthenticationState.HaveIttProvider);
        Assert.Equal(hasIttProvider ? ittProviderName : null, authStateHelper.AuthenticationState.IttProviderName);
    }
}
