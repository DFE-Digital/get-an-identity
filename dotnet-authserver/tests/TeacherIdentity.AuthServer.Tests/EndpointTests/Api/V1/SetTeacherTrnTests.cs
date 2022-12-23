using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api.V1;

public class SetTeacherTrnTests : TestBase
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
        await AssertEx.JsonResponseIsError(response, expectedErrorCode: 10001, expectedStatusCode: StatusCodes.Status400BadRequest);
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
        await AssertEx.JsonResponseIsError(response, expectedErrorCode: 10002, expectedStatusCode: StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Put_UserAlreadyHasTrn_ReturnsError()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(scope: PermittedScopes.First());

        var user = await TestData.CreateUser(hasTrn: true);
        var trn = TestData.GenerateTrn();

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
        await AssertEx.JsonResponseIsError(response, expectedErrorCode: 10006, expectedStatusCode: StatusCodes.Status400BadRequest);
    }

    [Theory]
    [MemberData(nameof(PermittedScopes))]
    public async Task Put_ValidRequestWithNonEmptyTrn_UpdatesUserAndReturnsNoContent(string scope)
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
            user = await dbContext.Users.SingleAsync(u => u.UserId == user.UserId);
            Assert.Equal(trn, user.Trn);
            Assert.Equal(TrnAssociationSource.Api, user.TrnAssociationSource);
            Assert.Equal(TrnLookupStatus.Found, user.TrnLookupStatus);
            Assert.Equal(Clock.UtcNow, user.Updated);
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                Assert.Equal(UserUpdatedEventSource.Api, userUpdatedEvent.Source);
                Assert.Equal(UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus, userUpdatedEvent.Changes);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            });
    }

    [Theory]
    [MemberData(nameof(PermittedScopes))]
    public async Task Put_ValidRequestWithEmptyTrn_UpdatesUserAndReturnsNoContent(string scope)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(scope);

        var user = await TestData.CreateUser(hasTrn: false);
        string? trn = null;

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
            user = await dbContext.Users.SingleAsync(u => u.UserId == user.UserId);
            Assert.Equal(trn, user.Trn);
            Assert.Null(user.TrnAssociationSource);
            Assert.Equal(TrnLookupStatus.Failed, user.TrnLookupStatus);
            Assert.Equal(Clock.UtcNow, user.Updated);
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                Assert.Equal(UserUpdatedEventSource.Api, userUpdatedEvent.Source);
                Assert.Equal(UserUpdatedEventChanges.TrnLookupStatus, userUpdatedEvent.Changes);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            });
    }

    public static TheoryData<string> InvalidTrnData { get; } = new TheoryData<string>()
    {
        { "" },
        { "0" },
        { "xxxxxxx" },
        { "123456" },
        { "12345678" }
    };

    public static TheoryData<string> NotPermittedScopes => ScopeTheoryData.GetAllStaffUserScopesExcept(PermittedScopes);

    public static TheoryData<string> PermittedScopes => ScopeTheoryData.Single(CustomScopes.UserWrite);
}
