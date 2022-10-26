using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
public class UpdateDetailsTests : TestBase
{
    public UpdateDetailsTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_RendersPreferredNameAndEmail()
    {
        // Arrange
        var user = await TestData.CreateUser();
        var authStateHelper = CreateAuthenticationStateHelper(user);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/update-details?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal($"{user.FirstName} {user.LastName}", doc.GetSummaryListValueForKey("Preferred name")?.Split("\n")[0]);
        Assert.Equal(user.EmailAddress, doc.GetSummaryListValueForKey("Email"));
    }

    [Fact]
    public async Task Get_ForDefaultUserType_RendersUpdateYourNameBlock()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        var authStateHelper = CreateAuthenticationStateHelper(user);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/update-details?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Contains(doc.GetElementsByTagName("h2"), h2 => h2.TextContent == "Update your name");
    }

    [Fact]
    public async Task Get_ForStaffUserType_DoesNotRenderUpdateYourNameBlock()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Staff);
        var authStateHelper = CreateAuthenticationStateHelper(user);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/update-details?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.DoesNotContain(doc.GetElementsByTagName("h2"), h2 => h2.TextContent == "Update your name");
    }

    private AuthenticationStateHelper CreateAuthenticationStateHelper(User user)
    {
        var scope = user.UserType == UserType.Staff ? CustomScopes.GetAnIdentityAdmin : null;

        var authenticationStateHelper = CreateAuthenticationStateHelper(
            authState =>
            {
                authState.OnEmailSet(user.EmailAddress);
                authState.OnEmailVerified(user);
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
