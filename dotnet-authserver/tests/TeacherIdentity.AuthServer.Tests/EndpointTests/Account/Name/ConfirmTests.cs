using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.Name;

public class ConfirmTests : TestBase
{
    public ConfirmTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NoFirstName_ReturnsBadRequest()
    {
        // Arrange
        var lastName = Faker.Name.Last();

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature($"/account/name/confirm?lastName={UrlEncode(lastName)}", "firstName", "lastName"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_NoLastName_ReturnsBadRequest()
    {
        // Arrange
        var firstName = Faker.Name.First();

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature($"/account/name/confirm?firstName={UrlEncode(firstName)}", "firstName", "lastName"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature($"/account/name/confirm?firstName={UrlEncode(firstName)}&lastName={UrlEncode(lastName)}", "firstName", "lastName"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_NoFirstName_ReturnsBadRequest()
    {
        // Arrange
        var lastName = Faker.Name.Last();

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/name/confirm?lastName={UrlEncode(lastName)}", "firstName", "lastName"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_NoLastName_ReturnsBadRequest()
    {
        // Arrange
        var firstName = Faker.Name.First();

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/name/confirm?firstName={UrlEncode(firstName)}", "firstName", "lastName"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidForm_UpdatesNameEmitsEventAndRedirectsToAccountPage()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var clientRedirectInfo = CreateClientRedirectInfo();

        var newFirstName = Faker.Name.First();
        var newLastName = Faker.Name.Last();

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/name/confirm?firstName={UrlEncode(newFirstName)}&lastName={UrlEncode(newLastName)}&{clientRedirectInfo.ToQueryParam()}", "firstName", "lastName"))
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/account?{clientRedirectInfo.ToQueryParam()}", response.Headers.Location?.OriginalString);

        user = await TestData.WithDbContext(dbContext => dbContext.Users.SingleAsync(u => u.UserId == user.UserId));
        Assert.Equal(newFirstName, user.FirstName);
        Assert.Equal(newLastName, user.LastName);
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

        var redirectedResponse = await response.FollowRedirect(HttpClient);
        var redirectedDoc = await redirectedResponse.GetDocument();
        AssertEx.HtmlDocumentHasFlashSuccess(redirectedDoc, "Your name has been updated");
    }

    private static string UrlEncode(string value) => UrlEncoder.Default.Encode(value);
}
