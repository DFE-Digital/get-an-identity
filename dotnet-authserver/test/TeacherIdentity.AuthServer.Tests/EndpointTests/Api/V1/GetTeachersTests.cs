using TeacherIdentity.AuthServer.Api.V1.Responses;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api.V1;

[Collection(nameof(DisableParallelization))]
public class GetTeachersTests : ApiTestBase, IAsyncLifetime
{
    public GetTeachersTests(HostFixture hostFixture)
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
        var response = await httpClient.GetAsync("/api/v1/teachers");

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(PermittedScopes))]
    public async Task Get_ValidRequest_ReturnsAllTeachers(string scope)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(scope);

        var user1 = await TestData.CreateUser();
        var user2 = await TestData.CreateUser();
        var user3 = await TestData.CreateUser();
        var sortedUsers = new[] { user1, user2, user3 }.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToArray();

        // Act
        var response = await httpClient.GetAsync("/api/v1/teachers");

        // Assert
        var responseObj = await AssertEx.JsonResponse<GetTeachersResponse>(response);

        Assert.Collection(
            responseObj.Teachers,
            user =>
            {
                Assert.Equal(sortedUsers[0].UserId, user.UserId);
                Assert.Equal(sortedUsers[0].EmailAddress, user.Email);
                Assert.Equal(sortedUsers[0].FirstName, user.FirstName);
                Assert.Equal(sortedUsers[0].LastName, user.LastName);
            },
            user =>
            {
                Assert.Equal(sortedUsers[1].UserId, user.UserId);
                Assert.Equal(sortedUsers[1].EmailAddress, user.Email);
                Assert.Equal(sortedUsers[1].FirstName, user.FirstName);
                Assert.Equal(sortedUsers[1].LastName, user.LastName);
            },
            user =>
            {
                Assert.Equal(sortedUsers[2].UserId, user.UserId);
                Assert.Equal(sortedUsers[2].EmailAddress, user.Email);
                Assert.Equal(sortedUsers[2].FirstName, user.FirstName);
                Assert.Equal(sortedUsers[2].LastName, user.LastName);
            });
    }

    public static TheoryData<string> NotPermittedScopes => ScopeTheoryData.GetAllAdminScopesExcept(PermittedScopes);

    public static TheoryData<string> PermittedScopes => ScopeTheoryData.Single(CustomScopes.GetAnIdentitySupport);
}
