using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class ElevateUserTrnVerificationTests : TestBase
{
    public ElevateUserTrnVerificationTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/users/{user.UserId}/elevate");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/users/{user.UserId}/elevate");
    }

    [Fact]
    public async Task Get_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var _ = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{userId}/elevate");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{userId}/elevate");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UserTrnVerificationLevelNotLow_ReturnsBadRequest()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Medium);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/elevate");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UserWithLowTrnVerificationLevel_UpdatesVerificationLevelRedirectsToUsers()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Low);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/elevate");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith($"/admin/users", response.Headers.Location?.OriginalString);
        await TestData.WithDbContext(async dbContext =>
        {
            var fetchedUser = await dbContext.Users.IgnoreQueryFilters().Where(u => u.UserId == user.UserId).SingleOrDefaultAsync();
            Assert.NotNull(fetchedUser);
            Assert.Null(fetchedUser.TrnVerificationLevel);
            Assert.Equal(TrnVerificationLevel.Medium, fetchedUser.EffectiveVerificationLevel);
            Assert.Equal(TrnAssociationSource.SupportUi, fetchedUser.TrnAssociationSource);
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userChangedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(Events.UserUpdatedEventChanges.TrnVerificationLevel | UserUpdatedEventChanges.TrnAssociationSource, userChangedEvent.Changes);
                Assert.Equal(user.UserId, userChangedEvent.User.UserId);
                Assert.Equal(Clock.UtcNow, userChangedEvent.CreatedUtc);
            });
    }
}
