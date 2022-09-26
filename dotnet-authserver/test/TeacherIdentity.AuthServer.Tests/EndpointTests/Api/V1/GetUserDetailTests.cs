using TeacherIdentity.AuthServer.Api.V1.Responses;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api.V1;

public class GetUserDetailTests : ApiTestBase
{
    public GetUserDetailTests(HostFixture hostFixture)
        : base(hostFixture)
    {
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
    public async Task Get_UserDoesNotExist_ReturnsNotFound(string scope)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(scope);
        var userId = Guid.NewGuid();

        // Act
        var response = await httpClient.GetAsync($"/api/v1/users/{userId}");

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(PermittedScopes))]
    public async Task Get_ValidRequest_ReturnsUser(string scope)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(scope);

        var user = await TestData.CreateUser(hasTrn: true);

        // Act
        var response = await httpClient.GetAsync($"/api/v1/users/{user.UserId}");

        // Assert
        var responseObj = await AssertEx.JsonResponse<GetUserDetailResponse>(response);

        Assert.Equal(user.UserId, responseObj.UserId);
        Assert.Equal(user.EmailAddress, responseObj.Email);
        Assert.Equal(user.FirstName, responseObj.FirstName);
        Assert.Equal(user.LastName, responseObj.LastName);
        Assert.Equal(user.Trn, responseObj.Trn);
    }

    public static TheoryData<string> NotPermittedScopes => ScopeTheoryData.GetAllAdminScopesExcept(PermittedScopes);

    public static TheoryData<string> PermittedScopes => ScopeTheoryData.Single(CustomScopes.GetAnIdentitySupport);
}
