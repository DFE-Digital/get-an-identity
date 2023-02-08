using AngleSharp.Dom;
using Url = Flurl.Url;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

[Collection(nameof(DisableParallelization))]
public class UsersTests : TestBase, IAsyncLifetime
{
    public UsersTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    public async Task InitializeAsync()
    {
        await ClearNonTestUsers();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
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

    [Theory]
    [MemberData(nameof(FilterCombinations))]
    public async Task Get_ValidRequest_ReturnsExpectedContent((bool active, TrnLookupStatus status)[] filters)
    {
        // Arrange
        var createdUserIds = new Dictionary<TrnLookupStatus, Guid>();
        foreach (var filter in filters)
        {
            createdUserIds.Add(filter.status, (await TestData.CreateUser(trnLookupStatus: filter.status)).UserId);
        }

        var uri = new Url("/admin/users");
        var expectedUserIds = createdUserIds.Values.ToList();

        var activeFilters = filters.Where(filter => filter.active).Select(filter => filter.status).ToArray();
        if (activeFilters.Length > 0)
        {
            uri.SetQueryParams(GetFilterQueryParams(activeFilters));
            expectedUserIds = activeFilters.Select(filter => createdUserIds[filter]).ToList();
        }

        var request = new HttpRequestMessage(HttpMethod.Get, uri);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();

        var userIds = GetUserIdsFromPane(doc.GetElementByTestId("moj-scrollable-pane")!)
            .Where(id => !TestUsers.All.Select(testUser => testUser.UserId).Contains(id))
            .ToList();

        expectedUserIds.Sort();
        userIds.Sort();

        Assert.Equal(expectedUserIds, userIds);

        static Guid[] GetUserIdsFromPane(IElement pane) =>
            pane.QuerySelectorAll("[data-testid^='user-']")
                .Select(e => e.GetAttribute("data-testid")!["user-".Length..])
                .Select(Guid.Parse)
                .ToArray();
    }

    public static IEnumerable<object[]> FilterCombinations
    {
        get
        {
            var values = new[] { true, false };
            var filters =
                from filterNone in values
                from filterPending in values
                from filterFound in values
                from filterFailed in values
                select new object[]
                {
                    new (bool filter, TrnLookupStatus status)[]
                    {
                        (filterNone, TrnLookupStatus.None),
                        (filterPending, TrnLookupStatus.Pending),
                        (filterFound, TrnLookupStatus.Found),
                        (filterFailed, TrnLookupStatus.Failed)
                    }
                };

            return filters;
        }
    }

    private Dictionary<string, string> GetFilterQueryParams(TrnLookupStatus[] activeFilters)
    {
        var filterParams = new Dictionary<string, string>();

        int count = 0;
        foreach (var status in activeFilters)
        {
            filterParams.Add($"LookupStatus[{count}]", status.ToString());
            count++;
        }

        return filterParams;
    }

    private async Task ClearNonTestUsers()
    {
        await TestData.WithDbContext(async dbContext =>
        {
            await TestUsers.DeleteNonTestUsers(dbContext);
        });
    }
}
