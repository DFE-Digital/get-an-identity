using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Authorization;

public class AuthorizeTests : TestBase
{
    public AuthorizeTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_FirstTimeUserWithTrn_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, userKnown: true, firstTimeUser: true, hasTrn: true);
        var request = new HttpRequestMessage(HttpMethod.Get, authStateHelper.AuthenticationState.AuthorizationUrl);

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
        var authStateHelper = await CreateAuthenticationStateHelper(HttpClient, userKnown: true, firstTimeUser: true, hasTrn: false);
        var request = new HttpRequestMessage(HttpMethod.Get, authStateHelper.AuthenticationState.AuthorizationUrl);

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
        var request = new HttpRequestMessage(HttpMethod.Get, authStateHelper.AuthenticationState.AuthorizationUrl);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.NotNull(doc.GetElementByTestId("known-user-content"));
    }

    private async Task<AuthenticationStateHelper> CreateAuthenticationStateHelper(
        HttpClient httpClient,
        bool userKnown = true,
        bool hasTrn = true,
        bool firstTimeUser = false)
    {
        var user = userKnown ? await TestData.CreateUser() : null;
        var trn = userKnown && hasTrn ? TestData.GenerateTrn() : null;

        var authenticationStateHelper = CreateAuthenticationStateHelper(authState =>
        {
            authState.EmailAddress = Faker.Internet.Email();
            authState.EmailAddressVerified = true;

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
                    authState.Trn = trn!;

                    A.CallTo(() => HostFixture.DqtApiClient!.GetTeacherIdentityInfo(user!.UserId))
                        .Returns(new DqtTeacherIdentityInfo() { Trn = authState.Trn, UserId = user!.UserId });
                }
            }
        });

        if (userKnown)
        {
            await HostFixture.SignInUser(authenticationStateHelper, httpClient, user!.UserId, firstTimeUser, trn);
        }

        return authenticationStateHelper;
    }
}
