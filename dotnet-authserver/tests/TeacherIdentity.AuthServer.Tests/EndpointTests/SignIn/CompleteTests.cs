using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
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
        var authStateHelper = await CreateAuthenticationStateHelper(firstTimeSignInForEmail: true, hasTrn: true);
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
        var authStateHelper = await CreateAuthenticationStateHelper(firstTimeSignInForEmail: true, hasTrn: false);
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
        var authStateHelper = await CreateAuthenticationStateHelper();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.NotNull(doc.GetElementByTestId("known-user-content"));
    }

    [Fact]
    public async Task Get_UserTypeIsDefault_ShowsTrnRow()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(scope: CustomScopes.Trn);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.NotNull(doc.GetElementByTestId("trn-row"));
    }

    [Fact]
    public async Task Get_UserTypeIsNotDefault_DoesNotShowTrnRow()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(userType: UserType.Staff, hasTrn: false, scope: CustomScopes.GetAnIdentityAdmin);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Null(doc.GetElementByTestId("trn-row"));
    }

    [Fact]
    public async Task Get_TrnIsNotKnown_RendersPlaceholderContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(hasTrn: false, firstTimeSignInForEmail: false, scope: CustomScopes.Trn);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.StartsWith("Awaiting name", doc.GetSummaryListValueForKey("Name"));
        Assert.Equal("Awaiting TRN", doc.GetSummaryListValueForKey("TRN"));
    }

    private async Task<AuthenticationStateHelper> CreateAuthenticationStateHelper(
        bool hasTrn = true,
        UserType userType = UserType.Default,
        bool firstTimeSignInForEmail = false,
        bool haveResumedCompletedJourney = false,
        string scope = "trn")
    {
        if (userType != UserType.Teacher && firstTimeSignInForEmail == true)
        {
            throw new ArgumentException("Cannot set firstTimeSignInForEmail = true for Staff users; we don't support registering Staff users.");
        }

        var user = await TestData.CreateUser(userType: userType, hasTrn: hasTrn);

        var authenticationStateHelper = CreateAuthenticationStateHelper(
            authState =>
            {
                authState.OnEmailSet(user.EmailAddress);

                if (userType == UserType.Default)
                {
                    authState.OnEmailVerified(user: null);
                    authState.OnTrnLookupCompletedAndUserRegistered(user, firstTimeSignInForEmail);
                }
                else
                {
                    authState.OnEmailVerified(user);
                }

                authState.EnsureOAuthState();
                authState.OAuthState.SetAuthorizationResponse(
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

        if (user.Trn is not null)
        {
            HostFixture.DqtApiClient
                .Setup(mock => mock.GetTeacherByTrn(user.Trn))
                .ReturnsAsync(new AuthServer.Services.DqtApi.TeacherInfo()
                {
                    Trn = user.Trn,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                });
        }

        return authenticationStateHelper;
    }
}
