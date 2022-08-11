namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class ConfirmationTests : TestBase
{
    public ConfirmationTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/confirmation");
    }

    [Fact]
    public async Task Get_UserNotKnown_ReturnsBadRequest()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, userKnown: false);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_FirstTimeUserWithTrn_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, userKnown: true, hasTrn: true, firstTimeUser: true);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/confirmation?{authStateHelper.ToQueryParam()}");

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
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, userKnown: true, hasTrn: false, firstTimeUser: true);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/confirmation?{authStateHelper.ToQueryParam()}");

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
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, userKnown: true);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.NotNull(doc.GetElementByTestId("known-user-content"));
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/confirmation");
    }

    [Fact]
    public async Task Post_UserNotKnown_ReturnsBadRequest()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, userKnown: false);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder().ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidRequest_UpdatedAuthenticationStateAndRedirects()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, userKnown: true);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder().ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.True(authStateHelper.AuthenticationState.HaveCompletedConfirmationPage);
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.AuthenticationState.AuthorizationUrl, response.Headers.Location?.OriginalString);
    }

    private async Task<AuthenticationStateHelper> CreateAuthenticationStateHelper(
        HttpClient httpClient,
        bool userKnown = true,
        bool hasTrn = true,
        bool firstTimeUser = false)
    {
        var user = userKnown ? (await TestData.CreateUser(trn: hasTrn ? TestData.GenerateTrn() : null)) : null;

        var authenticationStateHelper = CreateAuthenticationStateHelper(authState =>
        {
            authState.EmailAddress = Faker.Internet.Email();
            authState.EmailAddressConfirmed = true;

            if (userKnown)
            {
                authState.DateOfBirth = user!.DateOfBirth;
                authState.FirstName = user.FirstName;
                authState.LastName = user.LastName;
                authState.FirstTimeUser = firstTimeUser;
                authState.HaveCompletedFindALostTrnJourney = !userKnown && !firstTimeUser;
                authState.UserId = user.UserId;

                if (hasTrn)
                {
                    authState.Trn = user.Trn;
                }
            }
        });

        if (userKnown)
        {
            await HostFixture.SignInUser(user!.UserId, authenticationStateHelper, httpClient);
        }

        return authenticationStateHelper;
    }
}
