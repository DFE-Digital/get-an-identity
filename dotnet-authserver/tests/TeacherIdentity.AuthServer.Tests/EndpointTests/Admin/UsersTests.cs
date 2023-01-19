using AngleSharp.Dom;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class UsersTests : TestBase
{
    public UsersTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, "/admin/users/");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, "/admin/users");
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsExpectedContent()
    {
        // Arrange
        var userWithPendingTrnStatus = await TestData.CreateUser(trnLookupStatus: TrnLookupStatus.Pending);
        var userWithNoneTrnStatus = await TestData.CreateUser(trnLookupStatus: TrnLookupStatus.None);
        var userWithFoundTrnStatus = await TestData.CreateUser(trnLookupStatus: TrnLookupStatus.Found);
        var userWithFailedTrnStatus = await TestData.CreateUser(trnLookupStatus: TrnLookupStatus.Failed);

        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/users");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();

        var openUsers = GetUserIdsFromTab(doc.GetElementById("open-tab")!);
        var closedUsers = GetUserIdsFromTab(doc.GetElementById("closed-tab")!);

        Assert.Contains(userWithPendingTrnStatus.UserId, openUsers);
        Assert.DoesNotContain(userWithPendingTrnStatus.UserId, closedUsers);

        Assert.Contains(userWithNoneTrnStatus.UserId, closedUsers);
        Assert.DoesNotContain(userWithNoneTrnStatus.UserId, openUsers);

        Assert.Contains(userWithFoundTrnStatus.UserId, closedUsers);
        Assert.DoesNotContain(userWithFoundTrnStatus.UserId, openUsers);

        Assert.Contains(userWithFailedTrnStatus.UserId, closedUsers);
        Assert.DoesNotContain(userWithFailedTrnStatus.UserId, openUsers);

        static Guid[] GetUserIdsFromTab(IElement tab) =>
            tab.QuerySelectorAll("[data-testid^='user-']")
                .Select(e => e.GetAttribute("data-testid")!["user-".Length..])
                .Select(Guid.Parse)
                .ToArray();
    }
}
