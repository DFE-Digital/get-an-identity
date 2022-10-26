using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Authenticated;

public class UpdateNameTests : TestBase
{
    public UpdateNameTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_RendersCurrentNames()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var request = new HttpRequestMessage(HttpMethod.Get, "/update-name");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(user.FirstName, doc.GetElementByLabel("Preferred first name")?.GetAttribute("value"));
        Assert.Equal(user.LastName, doc.GetElementByLabel("Preferred last name")?.GetAttribute("value"));
    }

    [Theory]
    [MemberData(nameof(InvalidNamesData))]
    public async Task Post_InvalidNames_RendersError(
        string firstName,
        string lastName,
        string expectedErrorField,
        string expectedErrorMessage)
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var request = new HttpRequestMessage(HttpMethod.Post, "/update-name")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("FirstName", firstName)
                .Add("LastName", lastName)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, expectedErrorField, expectedErrorMessage);
    }

    [Fact]
    public async Task Post_ValidRequest_UpdatesUserEmitsEventAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var returnUrl = "/return-url";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/update-name?returnUrl={UrlEncoder.Default.Encode(returnUrl)}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("FirstName", firstName)
                .Add("LastName", lastName)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(returnUrl, response.Headers.Location?.OriginalString);

        user = await TestData.WithDbContext(dbContext => dbContext.Users.SingleAsync(u => u.UserId == user.UserId));
        Assert.Equal(firstName, user.FirstName);
        Assert.Equal(lastName, user.LastName);
        Assert.Equal(Clock.UtcNow, user.Updated);

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                Assert.Equal(UserUpdatedEventSource.ChangedByUser, userUpdatedEvent.Source);
                Assert.Equal(UserUpdatedEventChanges.FirstName | UserUpdatedEventChanges.LastName, userUpdatedEvent.Changes);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            });
    }

    public static TheoryData<string, string, string, string> InvalidNamesData { get; } = new()
    {
        {
            "Joe",
            "",
            "LastName",
            "Enter your preferred last name"
        },
        {
            "",
            "Bloggs",
            "FirstName",
            "Enter your preferred first name"
        },
        {
            new string('x', 201),
            "Bloggs",
            "FirstName",
            "Preferred first name must be 200 characters or less"
        },
        {
            "Joe",
            new string('x', 201),
            "LastName",
            "Preferred last name must be 200 characters or less"
        }
    };
}
