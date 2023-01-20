using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class EditUserNameTests : TestBase
{
    public EditUserNameTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/users/{user.UserId}/name");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/users/{user.UserId}/name");
    }

    [Fact]
    public async Task Get_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{userId}/name");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_UserIsNotDefaultType_ReturnsNotFound()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Staff);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/name");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/name");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal($"{user.FirstName} {user.LastName}", doc.GetSummaryListValueForKey("Preferred name"));
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, $"/admin/users/{user.UserId}/name");
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Post, $"/admin/users/{user.UserId}/name");
    }

    [Fact]
    public async Task Post_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{userId}/name");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UserIsNotDefaultType_ReturnsNotFound()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Staff);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/name");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidNamesData))]
    public async Task Post_InvalidName_RendersError(
        string newFirstName,
        string newLastName,
        string expectedErrorField,
        string expectedErrorMessage)
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/name")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewFirstName", newFirstName },
                { "NewLastName", newLastName },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, expectedErrorField, expectedErrorMessage);
    }

    [Theory]
    [InlineData(false, false, false, UserUpdatedEventChanges.None)]
    [InlineData(true, false, true, UserUpdatedEventChanges.FirstName)]
    [InlineData(false, true, true, UserUpdatedEventChanges.LastName)]
    [InlineData(true, true, true, UserUpdatedEventChanges.FirstName | UserUpdatedEventChanges.LastName)]
    public async Task Post_ValidRequest_SetsUserNameEmitsEventAndRedirects(
        bool changeFirstName,
        bool changeLastName,
        bool expectEvent,
        UserUpdatedEventChanges expectedChanges)
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        var newFirstName = changeFirstName ? Faker.Name.First() : user.FirstName;
        var newLastName = changeLastName ? Faker.Name.Last() : user.LastName;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/name")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewFirstName", newFirstName },
                { "NewLastName", newLastName },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/admin/users/{user.UserId}", response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            user = await dbContext.Users.SingleAsync(u => u.UserId == user.UserId);
            Assert.Equal(newFirstName, user.FirstName);
            Assert.Equal(newLastName, user.LastName);
        });

        if (expectEvent)
        {
            EventObserver.AssertEventsSaved(
                e =>
                {
                    var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                    Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                    Assert.Equal(UserUpdatedEventSource.SupportUi, userUpdatedEvent.Source);
                    Assert.Equal(expectedChanges, userUpdatedEvent.Changes);
                    Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
                    Assert.Equal(TestUsers.AdminUserWithAllRoles.UserId, userUpdatedEvent.UpdatedByUserId);
                });
        }
        else
        {
            EventObserver.AssertEventsSaved();
        }
    }

    public static TheoryData<string, string, string, string> InvalidNamesData { get; } = new()
    {
        { "", "Bloggs", "NewFirstName", "Enter a first name" },
        { "Joe", "", "NewLastName", "Enter a last name" },
        { new string('x', 201), "Bloggs", "NewFirstName", "First name must be 200 characters or less" },
        { "Joe", new string('x', 201), "NewLastName", "Last name must be 200 characters or less" },
    };
}
