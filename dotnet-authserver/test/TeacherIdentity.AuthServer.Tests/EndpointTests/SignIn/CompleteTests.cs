using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class CompleteTests : TestBase
{
    public CompleteTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_FirstTimeSignInForEmailWithTrn_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, firstTimeSignInForEmail: true, hasTrn: true);
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
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, firstTimeSignInForEmail: true, hasTrn: false);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");
        var client = TestClients.Client1;

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        var doc = await response.GetDocument();
        var panel = doc.GetElementByTestId("first-time-user-content");
        Assert.NotNull(panel);
        Assert.Null(doc.GetElementByTestId("known-trn-content"));
        Assert.NotNull(doc.GetElementByTestId("unknown-trn-content"));
        var titlecasedPostSignInMessage = string.Concat(char.ToUpper(client.PostSignInMessage![0]), client.PostSignInMessage!.Substring(1));
        Assert.Equal(titlecasedPostSignInMessage, doc.GetElementByTestId("first-time-user-postsigninmessage")?.TextContent);
    }

    [Fact]
    public async Task Get_KnownUser_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.NotNull(doc.GetElementByTestId("known-user-content"));
    }

    [Fact]
    public async Task Get_AuthorizationIsCompleted_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, haveResumedCompletedJourney: true);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");
        var client = TestClients.Client1;

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        var postsigninmessage = doc.GetElementByTestId("already-completed-postsigninmessage");
        Assert.NotNull(doc.GetElementByTestId("already-completed-content"));
        Assert.NotNull(postsigninmessage);
        Assert.Equal(client.PostSignInMessage, postsigninmessage?.TextContent);
    }

    [Fact]
    public async Task Get_AuthorizationRequestHasTrnScope_ShowsTrnRow()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, scope: CustomScopes.Trn);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.NotNull(doc.GetElementByTestId("trn-row"));
    }

    [Fact]
    public async Task Get_AuthorizationRequestDoesNotHaveTrnScope_DoesNotShowTrnRow()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, scope: CustomScopes.GetAnIdentityAdmin);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Null(doc.GetElementByTestId("trn-row"));
    }

    private async Task<AuthenticationStateHelper> CreateAuthenticationStateHelper(
        HttpClient httpClient,
        bool hasTrn = true,
        bool firstTimeSignInForEmail = false,
        bool haveResumedCompletedJourney = false,
        string scope = "trn")
    {
        var user = await TestData.CreateUser(hasTrn: hasTrn);

        var authenticationStateHelper = CreateAuthenticationStateHelper(
            authState =>
            {
                authState.OnEmailSet(user.EmailAddress);
                authState.OnEmailVerified(user);

                authState.SetAuthorizationResponse(
                    new[]
                    {
                        new KeyValuePair<string, string>("code", "abc"),
                        new KeyValuePair<string, string>("state", "syz")
                    },
                    responseMode: "form_post");

                if (haveResumedCompletedJourney)
                {
                    authState.OnHaveResumedCompletedJourney();
                }
            },
            scope);

        await HostFixture.SignInUser(authenticationStateHelper, httpClient, user!.UserId, firstTimeSignInForEmail);

        return authenticationStateHelper;
    }
}
