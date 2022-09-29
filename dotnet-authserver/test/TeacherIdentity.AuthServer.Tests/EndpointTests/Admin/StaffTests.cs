namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class StaffTests : TestBase
{
    public StaffTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, "/admin/staff");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/staff");

        // Act
        var response = await AuthenticatedHttpClientWithNoRoles!.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/staff");

        // Act
        var response = await AuthenticatedHttpClient!.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }
}
