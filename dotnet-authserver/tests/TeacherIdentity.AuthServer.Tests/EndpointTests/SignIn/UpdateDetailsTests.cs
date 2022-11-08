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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: false));
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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: false));
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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: false), additionalScopes: CustomScopes.GetAnIdentityAdmin);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/update-details?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.DoesNotContain(doc.GetElementsByTagName("h2"), h2 => h2.TextContent == "Update your name");
    }

    [Fact]
    public async Task Get_TrnIsNotKnown_RendersPlaceholderContent()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: false, userType: UserType.Default);
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: false));
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/update-details?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.StartsWith("Awaiting name", doc.GetSummaryListValueForKey("Name"));
    }
}
