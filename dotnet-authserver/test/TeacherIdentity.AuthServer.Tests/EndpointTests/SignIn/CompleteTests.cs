using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class CompleteTests : TestBase
{
    public CompleteTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_FirstTimeUserWithTrn_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, firstTimeUser: true, hasTrn: true);
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
    public async Task Get_FirstTimeUserWithoutTrn_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, firstTimeUser: true, hasTrn: false);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.NotNull(doc.GetElementByTestId("first-time-user-content"));
        Assert.Null(doc.GetElementByTestId("known-trn-content"));
        Assert.NotNull(doc.GetElementByTestId("unknown-trn-content"));
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

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.NotNull(doc.GetElementByTestId("already-completed-content"));
    }

    private async Task<AuthenticationStateHelper> CreateAuthenticationStateHelper(
        HttpClient httpClient,
        bool hasTrn = true,
        bool firstTimeUser = false,
        bool haveResumedCompletedJourney = false)
    {
        var user = await TestData.CreateUser();
        var trn = hasTrn ? TestData.GenerateTrn() : null;

        var authenticationStateHelper = CreateAuthenticationStateHelper(authState =>
        {
            authState.EmailAddress = Faker.Internet.Email();
            authState.EmailAddressVerified = true;

            authState.DateOfBirth = user!.DateOfBirth;
            authState.FirstName = user.FirstName;
            authState.LastName = user.LastName;
            authState.FirstTimeUser = firstTimeUser;
            authState.HaveCompletedTrnLookup = !firstTimeUser;
            authState.UserId = user.UserId;

            authState.RedirectUri = "https://dummy";
            authState.AuthorizationResponseMode = "form_post";
            authState.AuthorizationResponseParameters = new[]
            {
                new KeyValuePair<string, string>("code", "abc"),
                new KeyValuePair<string, string>("state", "syz")
            };

            authState.HaveResumedCompletedJourney = haveResumedCompletedJourney;

            if (hasTrn)
            {
                authState.Trn = trn!;

                A.CallTo(() => HostFixture.DqtApiClient!.GetTeacherIdentityInfo(user!.UserId))
                    .Returns(new DqtTeacherIdentityInfo() { Trn = authState.Trn, UserId = user!.UserId });
            }
        });

        await HostFixture.SignInUser(authenticationStateHelper, httpClient, user!.UserId, firstTimeUser, trn);

        return authenticationStateHelper;
    }
}
