using System.Net;
using TeacherIdentity.AuthServer.Api.V1.Responses;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api.V1;

[Collection(nameof(DisableParallelization))]
public class GetAllUsersTests : TestBase, IAsyncLifetime
{
    public GetAllUsersTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    private async Task ClearNonTestUsers()
    {
        await TestData.WithDbContext(async dbContext =>
        {
            var nonTestUsers = dbContext.Users.Where(u => !TestUsers.All.Select(u => u.UserId).Contains(u.UserId));
            dbContext.JourneyTrnLookupStates.RemoveRange(dbContext.JourneyTrnLookupStates);
            dbContext.Users.RemoveRange(nonTestUsers);
            await dbContext.SaveChangesAsync();
        });
    }

    [Theory]
    [MemberData(nameof(NotPermittedScopes))]
    public async Task Get_UserDoesNotHavePermission_ReturnsForbidden(string scope)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(scope);

        // Act
        var response = await httpClient.GetAsync("/api/v1/users");

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(PermittedScopes))]
    public async Task Get_ValidRequestWithoutPagingParameters_ReturnsAllTeachers(string scope)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(scope);

        var user1 = await TestData.CreateUser(hasTrn: true);
        var user2 = await TestData.CreateUser(hasTrn: true);
        var user3 = await TestData.CreateUser(hasTrn: false);
        var sortedUsers = new[] { user1, user2, user3 }.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToArray();

        // Act
        var response = await httpClient.GetAsync("/api/v1/users");

        // Assert
        var responseObj = await AssertEx.JsonResponse<GetAllUsersResponse>(response);

        // Remove any other test users
        var filteredUsers = responseObj.Users.Where(u => sortedUsers.Select(u => u.UserId).Contains(u.UserId));
        var filteredOutUsers = responseObj.Users.Where(u => !sortedUsers.Select(u => u.UserId).Contains(u.UserId));

        Assert.Collection(
            filteredUsers,
            user =>
            {
                Assert.Equal(sortedUsers[0].UserId, user.UserId);
                Assert.Equal(sortedUsers[0].EmailAddress, user.Email);
                Assert.Equal(sortedUsers[0].FirstName, user.FirstName);
                Assert.Equal(sortedUsers[0].LastName, user.LastName);
                Assert.Equal(sortedUsers[0].Trn, user.Trn);
            },
            user =>
            {
                Assert.Equal(sortedUsers[1].UserId, user.UserId);
                Assert.Equal(sortedUsers[1].EmailAddress, user.Email);
                Assert.Equal(sortedUsers[1].FirstName, user.FirstName);
                Assert.Equal(sortedUsers[1].LastName, user.LastName);
                Assert.Equal(sortedUsers[1].Trn, user.Trn);
            },
            user =>
            {
                Assert.Equal(sortedUsers[2].UserId, user.UserId);
                Assert.Equal(sortedUsers[2].EmailAddress, user.Email);
                Assert.Equal(sortedUsers[2].FirstName, user.FirstName);
                Assert.Equal(sortedUsers[2].LastName, user.LastName);
                Assert.Equal(sortedUsers[2].Trn, user.Trn);
            });

        //Total is filtered out test users + expected users
        Assert.Equal(filteredUsers.Count() + filteredOutUsers.Count(), responseObj.Total);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1000)]
    public async Task Get_WithInvalidPageSize_ReturnsBadRequest(int pageSize)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(CustomScopes.UserRead);

        // Act
        var response = await httpClient.GetAsync($"/api/v1/users?PageNumber=1&PageSize={pageSize}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Get_WithInvalidPageNumber_ReturnsBadRequest(int page)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(CustomScopes.UserRead);

        // Act
        var response = await httpClient.GetAsync($"/api/v1/users?PageNumber={page}&PageSize=50");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequestWithOutOfRangePageNumber_ReturnsEmptyCollection()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(CustomScopes.UserWrite);

        var user1 = await TestData.CreateUser(hasTrn: true);
        var user2 = await TestData.CreateUser(hasTrn: true);
        var user3 = await TestData.CreateUser(hasTrn: false);
        var sortedUsers = new[] { user1, user2, user3 }.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToArray();

        // Act
        var response = await httpClient.GetAsync("/api/v1/users?PageNumber=1000&PageSize=40");

        // Assert
        var responseObj = await AssertEx.JsonResponse<GetAllUsersResponse>(response);

        // Remove any other test users
        var filteredUsers = responseObj.Users.Where(u => sortedUsers.Select(u => u.UserId).Contains(u.UserId));
        var filteredOutUsers = responseObj.Users.Where(u => !sortedUsers.Select(u => u.UserId).Contains(u.UserId));

        Assert.Empty(filteredUsers);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Get_ValidRequestWithValidPageRange_ReturnsCorrectPage(int page)
    {

        // Arrange
        var httpClient = await CreateHttpClientWithToken(CustomScopes.UserWrite);
        var pageSize = 3;
        var skip = (page - 1) * pageSize;

        var user1 = await TestData.CreateUser(hasTrn: true);
        var user2 = await TestData.CreateUser(hasTrn: true);
        var user3 = await TestData.CreateUser(hasTrn: false);
        var user4 = await TestData.CreateUser(hasTrn: false);
        var user5 = await TestData.CreateUser(hasTrn: false);
        var user6 = await TestData.CreateUser(hasTrn: false);
        var user7 = await TestData.CreateUser(hasTrn: false);
        var sortedUsers = new[] { user1, user2, user3, user4, user5, user6, user7, TestUsers.DefaultUser }.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToArray();

        var pagedUsers = sortedUsers.Skip(skip).Take(pageSize).ToArray();

        // Act
        var response = await httpClient.GetAsync($"/api/v1/users?PageNumber={page}&PageSize={pageSize}");

        // Assert
        var responseObj = await AssertEx.JsonResponse<GetAllUsersResponse>(response);
        Assert.Collection(
           responseObj.Users,
           user =>
           {
               Assert.Equal(pagedUsers[0].UserId, user.UserId);
               Assert.Equal(pagedUsers[0].EmailAddress, user.Email);
               Assert.Equal(pagedUsers[0].FirstName, user.FirstName);
               Assert.Equal(pagedUsers[0].LastName, user.LastName);
               Assert.Equal(pagedUsers[0].Trn, user.Trn);
           },
           user =>
           {
               Assert.Equal(pagedUsers[1].UserId, user.UserId);
               Assert.Equal(pagedUsers[1].EmailAddress, user.Email);
               Assert.Equal(pagedUsers[1].FirstName, user.FirstName);
               Assert.Equal(pagedUsers[1].LastName, user.LastName);
               Assert.Equal(pagedUsers[1].Trn, user.Trn);
           },
           user =>
           {
               Assert.Equal(pagedUsers[2].UserId, user.UserId);
               Assert.Equal(pagedUsers[2].EmailAddress, user.Email);
               Assert.Equal(pagedUsers[2].FirstName, user.FirstName);
               Assert.Equal(pagedUsers[2].LastName, user.LastName);
               Assert.Equal(pagedUsers[2].Trn, user.Trn);
           });
        Assert.Equal(sortedUsers.Count(), responseObj.Total);
    }

    public async Task InitializeAsync()
    {
        await ClearNonTestUsers();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public static TheoryData<string> NotPermittedScopes => ScopeTheoryData.GetAllStaffUserScopesExcept(PermittedScopes);

    public static TheoryData<string> PermittedScopes => ScopeTheoryData.FromScopes(CustomScopes.UserRead, CustomScopes.UserWrite);
}
