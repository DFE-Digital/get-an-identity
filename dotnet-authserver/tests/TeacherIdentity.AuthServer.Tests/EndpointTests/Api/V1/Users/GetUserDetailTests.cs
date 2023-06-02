using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Pages.SignIn.Trn;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api.V1.Users;

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
        var httpClient = await CreateHttpClientWithToken(withUser: true, scope);

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
        var httpClient = await CreateHttpClientWithToken(withUser: true, scope);
        var userId = Guid.NewGuid();

        // Act
        var response = await httpClient.GetAsync($"/api/v1/users/{userId}");

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(PermittedScopes))]
    public async Task Get_MergedUser_ReturnsUserMergedInto(string scope)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(withUser: true, scope);

        var registeredWithClient = TestClients.DefaultClient;
        var user = await TestData.CreateUser(hasTrn: true, registeredWithClientId: registeredWithClient.ClientId);
        var mergedUser = await TestData.CreateUser(hasTrn: true, registeredWithClientId: registeredWithClient.ClientId, mergedWithUserId: user.UserId);

        // Act
        var response = await httpClient.GetAsync($"/api/v1/users/{mergedUser.UserId}");

        // Assert
        await AssertEx.JsonResponseEquals(
            response,
            new
            {
                userId = user.UserId,
                email = user.EmailAddress,
                firstName = user.FirstName,
                middleName = user.MiddleName,
                lastName = user.LastName,
                preferredName = user.PreferredName,
                dateOfBirth = user.DateOfBirth,
                trn = user.Trn,
                trnLookupStatus = user.TrnLookupStatus,
                mobileNumber = user.MobileNumber,
                created = user.Created,
                registeredWithClientId = registeredWithClient.ClientId,
                registeredWithClientDisplayName = registeredWithClient.DisplayName,
                mergedUserIds = new[] { mergedUser.UserId }
            });
    }

    [Theory]
    [MemberData(nameof(PermittedScopes))]
    public async Task Get_ValidRequest_ReturnsUser(string scope)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(withUser: true, scope);

        var registeredWithClient = TestClients.DefaultClient;
        var user = await TestData.CreateUser(hasTrn: true, registeredWithClientId: registeredWithClient.ClientId, hasPreferredName: true);

        // Act
        var response = await httpClient.GetAsync($"/api/v1/users/{user.UserId}");

        // Assert
        await AssertEx.JsonResponseEquals(
            response,
            new
            {
                userId = user.UserId,
                email = user.EmailAddress,
                firstName = user.FirstName,
                middleName = user.MiddleName,
                lastName = user.LastName,
                preferredName = user.PreferredName,
                dateOfBirth = user.DateOfBirth,
                trn = user.Trn,
                trnLookupStatus = user.TrnLookupStatus,
                mobileNumber = user.MobileNumber,
                created = user.Created,
                registeredWithClientId = registeredWithClient.ClientId,
                registeredWithClientDisplayName = registeredWithClient.DisplayName,
                mergedUserIds = Array.Empty<object>()
            });
    }

    public static TheoryData<string> NotPermittedScopes => ScopeTheoryData.GetAllStaffUserScopesExcept(PermittedScopes);

    public static TheoryData<string> PermittedScopes => ScopeTheoryData.FromScopes(CustomScopes.UserRead, CustomScopes.UserWrite);
}
