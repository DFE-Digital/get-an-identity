using Microsoft.Extensions.Caching.Memory;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
public class IttProviderTests : TestBase
{
    public IttProviderTests(HostFixture hostFixture)
        : base(hostFixture)
    {
        HostFixture.DqtApiClient.Setup(mock => mock.GetIttProviders(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetIttProvidersResponse()
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
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, HttpMethod.Get, "/sign-in/trn/itt-provider");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, CustomScopes.DqtRead, HttpMethod.Get, "/sign-in/trn/itt-provider");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Get_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, HttpMethod.Get, "/sign-in/trn/itt-provider");
    }

    [Fact]
    public async Task Get_AwardedQtsNotSet_RedirectsToAwardedQtsPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.NationalInsuranceNumberSet(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/itt-provider?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/awarded-qts", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_StoresIttProviderNamesInCache()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.DqtRead);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/itt-provider?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var ittProviderNames = HostFixture.Services.GetService<IMemoryCache>()?.Get<string[]>("IttProviderNames");
        Assert.Equal(new[] { "provider 1", "provider 2" }, ittProviderNames);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/itt-provider");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/itt-provider");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, HttpMethod.Post, "/sign-in/trn/itt-provider");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, CustomScopes.DqtRead, HttpMethod.Post, "/sign-in/trn/itt-provider");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Post_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, HttpMethod.Post, "/sign-in/trn/itt-provider");
    }

    [Fact]
    public async Task Post_AwardedQtsNotSet_RedirectsToAwardedQtsPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.NationalInsuranceNumberSet(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/itt-provider?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/awarded-qts", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_NullHasIttProvider_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.DqtRead);
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
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.DqtRead);
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
    public async Task Post_ValidForm_UpdatesAuthenticationStateRedirectsToCheckAnswers(bool hasIttProvider)
    {
        // Arrange
        var ittProviderName = "provider";

        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.DqtRead);

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
        Assert.StartsWith("/sign-in/trn/check-answers", response.Headers.Location?.OriginalString);

        Assert.Equal(hasIttProvider, authStateHelper.AuthenticationState.HasIttProvider);
        Assert.Equal(hasIttProvider ? ittProviderName : null, authStateHelper.AuthenticationState.IttProviderName);
    }

    [Fact]
    public async Task Post_TrnLookupFindsExactlyOneResult_RedirectsToCheckAnswersPage()
    {
        // Arrange
        var ittProviderName = "provider";

        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.DqtRead);
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/itt-provider?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasIttProvider", true },
                { "IttProviderName", ittProviderName },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/check-answers", response.Headers.Location?.OriginalString);
    }

    private Func<AuthenticationState, Task> ConfigureValidAuthenticationState(AuthenticationStateHelper.Configure configure) =>
        configure.Trn.AwardedQtsSet(awardedQts: true);
}
