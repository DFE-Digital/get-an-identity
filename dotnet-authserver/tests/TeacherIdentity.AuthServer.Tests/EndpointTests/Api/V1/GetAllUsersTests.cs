using System.Net;
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
        var allUsers = new[] { user1, user2, user3 }.Append(TestUsers.DefaultUser).OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToArray();

        // Act
        var response = await httpClient.GetAsync("/api/v1/users");

        // Assert
        var responseObj = await AssertEx.JsonResponse(response);
        var responseUsers = responseObj.RootElement.GetProperty("users").EnumerateArray();

        foreach (var (responseUser, modelUser) in responseUsers.Zip(allUsers, (response, model) => (Response: response, Model: model)))
        {
            Assert.Equal(modelUser.UserId, responseUser.GetProperty("userId").GetGuid());
            Assert.Equal(modelUser.EmailAddress, responseUser.GetProperty("email").GetString());
            Assert.Equal(modelUser.FirstName, responseUser.GetProperty("firstName").GetString());
            Assert.Equal(modelUser.LastName, responseUser.GetProperty("lastName").GetString());
            Assert.Equal(modelUser.Trn, responseUser.GetProperty("trn").GetString());
        }

        Assert.Equal(allUsers.Length, responseObj.RootElement.GetProperty("total").GetInt32());
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
        var response = await httpClient.GetAsync($"/api/v1/users?pageNumber=1&pageSize={pageSize}");

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
        var response = await httpClient.GetAsync($"/api/v1/users?pageNumber={page}&pageSize=50");

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
        var response = await httpClient.GetAsync("/api/v1/users?pageNumber=1000&pageSize=40");

        // Assert
        var responseObj = await AssertEx.JsonResponse(response);
        var returnedUsers = responseObj.RootElement.GetProperty("users").EnumerateArray();
        Assert.Empty(returnedUsers);
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
        var response = await httpClient.GetAsync($"/api/v1/users?pageNumber={page}&pageSize={pageSize}");

        // Assert
        var responseObj = await AssertEx.JsonResponse(response);

        Assert.Collection(
           responseObj.RootElement.GetProperty("users").EnumerateArray(),
           user => Assert.Equal(pagedUsers[0].UserId, user.GetProperty("userId").GetGuid()),
           user => Assert.Equal(pagedUsers[1].UserId, user.GetProperty("userId").GetGuid()),
           user => Assert.Equal(pagedUsers[2].UserId, user.GetProperty("userId").GetGuid()));

        Assert.Equal(sortedUsers.Count(), responseObj.RootElement.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Get_ValidRequestWithTrnLookupStatusFilter_OnlyReturnsUsersMatchingTrnLookupStatus()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(CustomScopes.UserWrite);

        var userWithNoneStatus = await TestData.CreateUser(trnLookupStatus: TrnLookupStatus.None);
        var userWithPendingStatus = await TestData.CreateUser(trnLookupStatus: TrnLookupStatus.Pending);
        var userWithFoundStatus = await TestData.CreateUser(trnLookupStatus: TrnLookupStatus.Found);
        var userWithFailedStatus = await TestData.CreateUser(trnLookupStatus: TrnLookupStatus.Failed);

        // Act
        var response = await httpClient.GetAsync($"/api/v1/users?trnLookupStatus=None,Failed");

        // Assert
        var responseObj = await AssertEx.JsonResponse(response);
        var userIds = responseObj.RootElement.GetProperty("users").EnumerateArray().Select(u => u.GetProperty("userId").GetGuid());

        Assert.Contains(userWithNoneStatus.UserId, userIds);
        Assert.Contains(userWithFailedStatus.UserId, userIds);
        Assert.DoesNotContain(userWithPendingStatus.UserId, userIds);
        Assert.DoesNotContain(userWithFoundStatus.UserId, userIds);
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
