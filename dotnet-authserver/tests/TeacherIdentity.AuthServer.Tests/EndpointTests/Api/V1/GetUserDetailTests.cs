using TeacherIdentity.AuthServer.Api.V1.Responses;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api.V1;

public class GetUserDetailTests : TestBase
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

        var registeredWithClient = TestClients.Client1;
        var user = await TestData.CreateUser(hasTrn: true, registeredWithClientId: registeredWithClient.ClientId);

        // Act
        var response = await httpClient.GetAsync($"/api/v1/users/{user.UserId}");

        // Assert
        var responseObj = await AssertEx.JsonResponse<GetUserDetailResponse>(response);

        Assert.Equal(user.UserId, responseObj.UserId);
        Assert.Equal(user.EmailAddress, responseObj.Email);
        Assert.Equal(user.FirstName, responseObj.FirstName);
        Assert.Equal(user.LastName, responseObj.LastName);
        Assert.Equal(user.Trn, responseObj.Trn);
        Assert.Equal(user.Created, responseObj.Created);
        Assert.Equal(registeredWithClient.ClientId, responseObj.RegisteredWithClientId);
        Assert.Equal(registeredWithClient.DisplayName, responseObj.RegisteredWithClientDisplayName);
    }

    public static TheoryData<string> NotPermittedScopes => ScopeTheoryData.GetAllStaffUserScopesExcept(PermittedScopes);

    public static TheoryData<string> PermittedScopes => ScopeTheoryData.FromScopes(CustomScopes.UserRead, CustomScopes.UserWrite);
}
