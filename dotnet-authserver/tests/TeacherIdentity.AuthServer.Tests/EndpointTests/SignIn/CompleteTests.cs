using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class CompleteTests : TestBase
{
    public CompleteTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/complete");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/complete");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_DoesNotRedirectToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_DoesNotRedirectToPostSignInUrl(additionalScopes: null, HttpMethod.Get, "/sign-in/complete");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        var user = await TestData.CreateUser(hasTrn: true);
        await JourneyHasExpired_RendersErrorPage(c => c.Completed(user), additionalScopes: null, HttpMethod.Get, "/sign-in/complete");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData()]
    public async Task Get_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, HttpMethod.Get, "/sign-in/complete");
    }

    [Fact]
    public async Task Get_FirstTimeSignInForEmailWithTrn_RendersExpectedContent()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: true), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.NotNull(doc.GetElementByTestId("first-time-user-content"));
        Assert.NotNull(doc.GetElementByTestId("known-trn-content"));
        Assert.Null(doc.GetElementByTestId("unknown-trn-content"));
    }

    [Fact]
    public async Task Get_FirstTimeSignInForEmailWithoutTrn_RendersExpectedContent()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: false);
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: true), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        var doc = await response.GetDocument();
        Assert.Null(doc.GetElementByTestId("known-trn-content"));
        Assert.NotNull(doc.GetElementByTestId("unknown-trn-content"));
    }

    [Fact]
    public async Task Get_KnownUser_RendersExpectedContent()
    {
        // Arrange
        var user = await TestData.CreateUser();
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: false), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.NotNull(doc.GetElementByTestId("known-user-content"));
    }

    [Theory]
    [MemberData(nameof(FirstTimeSignInForRegisterForNpqData))]
    public async Task Get_FirstTimeSignInForEmailWithoutTrnUsingRegisterForNpqClient_RendersExpectedContent(
        string additionalScopes,
        string[] expectedContent)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: false);
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: true), additionalScopes, TestClients.RegisterForNpq);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        var content = doc.GetElementByTestId("first-time-user-content")?.InnerHtml;
        Assert.Contains("Continue to register for an NPQ", content);

        foreach (var block in expectedContent)
        {
            Assert.Contains(block, content);
        }
    }

    [Fact]
    public async Task Get_FirstTimeSignInForEmailWithoutTrnUsingDefaultClient_RendersExpectedContent()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: false);
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: true), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Contains("We’ve finished checking our records", doc.GetElementByTestId("first-time-user-content")!.InnerHtml);
        Assert.Contains("You can continue anyway and we’ll try to find your record. Someone may be in touch to ask for more information.", doc.GetElementByTestId("first-time-user-content")!.InnerHtml);
    }

    public static TheoryData<string, string[]> FirstTimeSignInForRegisterForNpqData => new()
    {
        {
#pragma warning disable CS0618 // Type or member is obsolete
            CustomScopes.Trn,
#pragma warning restore CS0618 // Type or member is obsolete
            new[]
            {
                "Although we could not find your record, you can continue to register for an NPQ.",
                "You’ll need to enter some of your details again."
            }
        },
        {
            CustomScopes.DqtRead,
            new[]
            {
                "You can continue anyway and we’ll try to find your record. Someone may be in touch to ask for more information.",
                "If you need to come back to this service later you’ll only need to give us your email address"
            }
        }
    };
}
