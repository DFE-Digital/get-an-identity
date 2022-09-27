using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api.V1;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
public class SetTeacherTrnTests : ApiTestBase
{
    public SetTeacherTrnTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Theory]
    [MemberData(nameof(NotPermittedScopes))]
    public async Task Put_UserDoesNotHavePermission_ReturnsForbidden(string scope)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(scope);

        var user = await TestData.CreateUser(hasTrn: false);
        var trn = "1234567";

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/users/{user.UserId}/trn")
        {
            Content = JsonContent.Create(new
            {
                Trn = trn
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    [Fact]
    public async Task Put_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(scope: PermittedScopes.First());

        var userId = Guid.NewGuid();
        var trn = "1234567";

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/users/{userId}/trn")
        {
            Content = JsonContent.Create(new
            {
                Trn = trn
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Put_UserIsNotTeacher_ReturnsError()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(scope: PermittedScopes.First());

        var user = await TestData.CreateUser(userType: Models.UserType.Staff);
        var trn = "1234567";

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/users/{user.UserId}/trn")
        {
            Content = JsonContent.Create(new
            {
                Trn = trn
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        await AssertEx.ResponseIsError(response, expectedErrorCode: 10001, expectedStatusCode: StatusCodes.Status400BadRequest);
    }

    [Theory]
    [MemberData(nameof(InvalidTrnData))]
    public async Task Put_TrnIsInvalid_ReturnsError(string trn)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(scope: PermittedScopes.First());

        var user = await TestData.CreateUser(hasTrn: false);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/users/{user.UserId}/trn")
        {
            Content = JsonContent.Create(new
            {
                Trn = trn
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Put_TrnIsAlreadyInUse_ReturnsError()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(scope: PermittedScopes.First());

        var user = await TestData.CreateUser(hasTrn: false);
        var otherUser = await TestData.CreateUser(hasTrn: true);
        var trn = otherUser.Trn!;

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/users/{user.UserId}/trn")
        {
            Content = JsonContent.Create(new
            {
                Trn = trn
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        await AssertEx.ResponseIsError(response, expectedErrorCode: 10002, expectedStatusCode: StatusCodes.Status400BadRequest);
    }

    [Theory]
    [MemberData(nameof(PermittedScopes))]
    public async Task Put_ValidRequest_UpdatesUserAndCallsDqtApiAndReturnsNoContent(string scope)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(scope);

        var user = await TestData.CreateUser(hasTrn: false);
        var trn = "1234567";

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/users/{user.UserId}/trn")
        {
            Content = JsonContent.Create(new
            {
                Trn = trn
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, (int)response.StatusCode);

        await TestData.WithDbContext(async dbContext =>
        {
            user = await dbContext.Users.SingleOrDefaultAsync(u => u.UserId == user.UserId);
            Assert.Equal(trn, user?.Trn);
        });

        HostFixture.DqtApiClient
            .Verify(mock => mock.SetTeacherIdentityInfo(It.Is<DqtTeacherIdentityInfo>(x => x.UserId == user!.UserId && x.Trn == trn)), Times.Once);
    }

    public static TheoryData<string> InvalidTrnData { get; } = new TheoryData<string>()
    {
        { "" },
        { "0" },
        { "xxxxxxx" },
        { "123456" },
        { "12345678" }
    };

    public static TheoryData<string> NotPermittedScopes => ScopeTheoryData.GetAllAdminScopesExcept(PermittedScopes);

    public static TheoryData<string> PermittedScopes => ScopeTheoryData.Single(CustomScopes.GetAnIdentitySupport);
}