using TeacherIdentity.AuthServer.Api.V1.Responses;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api.V1;

[Collection(nameof(DisableParallelization))]
public class GetAllUsersTests : ApiTestBase, IAsyncLifetime
{
    public GetAllUsersTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        await HostFixture.DbHelper.ClearData();
        await HostFixture.ConfigureTestUsers();
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
    public async Task Get_ValidRequest_ReturnsAllTeachers(string scope)
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

        Assert.Collection(
            responseObj.Users,
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
    }

    public static TheoryData<string> NotPermittedScopes => ScopeTheoryData.GetAllAdminScopesExcept(PermittedScopes);

    public static TheoryData<string> PermittedScopes => ScopeTheoryData.Single(CustomScopes.GetAnIdentitySupport);
}
