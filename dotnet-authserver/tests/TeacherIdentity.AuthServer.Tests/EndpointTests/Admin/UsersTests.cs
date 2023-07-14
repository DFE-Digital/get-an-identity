using System.Reflection;
using AngleSharp.Dom;
using TeacherIdentity.AuthServer.Models;
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
    [MemberData(nameof(UserSearchProperty))]
    public async Task Get_ValidRequestByUserPropertySearch_ReturnsExpectedContent(PropertyInfo property)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        var otherUser = await TestData.CreateUser(hasTrn: true);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users?userSearch={property.GetValue(user)}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();

        var userIds = GetUserIdsFromPane(doc.GetElementByTestId("moj-scrollable-pane")!)
            .Where(id => !TestUsers.All.Select(testUser => testUser.UserId).Contains(id))
            .ToList();

        Assert.Contains(user.UserId, userIds);
        Assert.DoesNotContain(otherUser.UserId, userIds);
    }

    public static TheoryData<PropertyInfo> UserSearchProperty { get; } = new()
    {
        typeof(User).GetProperty("FirstName")!,
        typeof(User).GetProperty("LastName")!,
        typeof(User).GetProperty("Trn")!,
        typeof(User).GetProperty("EmailAddress")!,
    };

    [Fact]
    public async Task Get_ValidRequestByUserFirstAndLastNameSearch_ReturnsExpectedContent()
    {
        // Arrange
        var user = await TestData.CreateUser();
        var otherUser = await TestData.CreateUser(firstName: user.FirstName);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users?userSearch={user.FirstName} {user.LastName}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();

        var userIds = GetUserIdsFromPane(doc.GetElementByTestId("moj-scrollable-pane")!)
            .Where(id => !TestUsers.All.Select(testUser => testUser.UserId).Contains(id))
            .ToList();

        Assert.Contains(user.UserId, userIds);
        Assert.DoesNotContain(otherUser.UserId, userIds);
    }

    [Fact]
    public async Task Get_ValidRequestAndNoUsersFound_RendersNoUsersFound()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users?userSearch=abc&&LookupStatus=Found");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();

        Assert.Contains("No users found", doc.GetElementByTestId("moj-scrollable-pane")!.InnerHtml);
    }

    [Theory]
    [MemberData(nameof(FilterCombination))]
    public async Task Get_ValidRequestWithLookupStatusFilter_ReturnsExpectedContent((bool active, TrnLookupStatus status)[] filters, bool withSupportTicket)
    {
        // Arrange
        var createdUserIds = new Dictionary<TrnLookupStatus, Guid>();
        foreach (var filter in filters)
        {
            createdUserIds.Add(filter.status, (await TestData.CreateUser(trnLookupStatus: filter.status, trnLookupSupportTicketCreated: withSupportTicket)).UserId);
        }

        var uri = new Url("/admin/users");
        var expectedUserIds = createdUserIds.Values.ToList();

        var activeFilters = filters.Where(filter => filter.active).Select(filter => filter.status).ToArray();
        if (activeFilters.Length > 0)
        {
            uri.SetQueryParams(GetFilterQueryParams(activeFilters, withSupportTicket));
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
    }

    public static IEnumerable<object[]> FilterCombination
    {
        get
        {
            var values = new[] { true, false };
            var filters =
                from filterNone in values
                from filterPending in values
                from filterFound in values
                from filterFailed in values
                from withSupportTicket in values
                select new object[]
                {
                    new (bool filter, TrnLookupStatus status)[]
                    {
                        (filterNone, TrnLookupStatus.None),
                        (filterPending, TrnLookupStatus.Pending),
                        (filterFound, TrnLookupStatus.Found),
                        (filterFailed, TrnLookupStatus.Failed)
                    },
                    withSupportTicket
                };

            return filters;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_ValidRequest_ReturnsExpectedContentForUser(bool hasTrn)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: hasTrn);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();

        var userRow = doc.GetElementByTestId($"user-{user.UserId}")!;

        if (hasTrn)
        {
            Assert.DoesNotContain("Assign TRN", userRow.InnerHtml);
        }
        else
        {
            Assert.Contains("Assign TRN", userRow.InnerHtml);
        }
    }

    private Dictionary<string, string> GetFilterQueryParams(TrnLookupStatus[] activeFilters, bool withSupportTicket)
    {
        var filterParams = new Dictionary<string, string>();

        int count = 0;
        foreach (var status in activeFilters)
        {
            filterParams.Add($"LookupStatus[{count}]", status.ToString());
            count++;
        }

        filterParams.Add("WithSupportTicket", withSupportTicket ? "true" : "false");
        return filterParams;
    }

    private static Guid[] GetUserIdsFromPane(IElement pane) =>
        pane.QuerySelectorAll("[data-testid^='user-']")
            .Select(e => e.GetAttribute("data-testid")!["user-".Length..])
            .Select(Guid.Parse)
            .ToArray();

    private async Task ClearNonTestUsers()
    {
        await TestData.WithDbContext(async dbContext =>
        {
            await TestUsers.DeleteNonTestUsers(dbContext);
        });
    }
}
