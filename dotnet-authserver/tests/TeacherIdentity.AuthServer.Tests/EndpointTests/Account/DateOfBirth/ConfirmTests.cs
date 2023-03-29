using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.DateOfBirth;

public class ConfirmTests : TestBase
{
    public ConfirmTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NoDateOfBirth_ReturnsBadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature($"/account/date-of-birth/confirm", "dateOfBirth"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_InvalidDateOfBirth_ReturnsBadRequest()
    {
        // Arrange
        var dateOfBirth = (DateOnly?)null;

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature($"/account/date-of-birth/confirm?dateOfBirth={UrlEncode(dateOfBirth?.ToString("yyyy-MM-dd") ?? string.Empty)}", "dateOfBirth"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsSuccess()
    {
        var dateOfBirth = new DateOnly(2000, 1, 1);

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature($"/account/date-of-birth/confirm?dateOfBirth={UrlEncode(dateOfBirth.ToString("yyyy-MM-dd"))}", "dateOfBirth"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_NoDateOfBirth_ReturnsBadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/date-of-birth/confirm", "dateOfBirth"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidForm_UpdatesDateOfBirthEmitsEventAndRedirectsToAccountPage()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var clientRedirectInfo = CreateClientRedirectInfo();

        var newDateOfBirth = new DateOnly(2000, 1, 1);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/date-of-birth/confirm?dateOfBirth={UrlEncode(newDateOfBirth.ToString("yyyy-MM-dd"))}&{clientRedirectInfo.ToQueryParam()}", "dateOfBirth"))
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/account?{clientRedirectInfo.ToQueryParam()}", response.Headers.Location?.OriginalString);

        user = await TestData.WithDbContext(dbContext => dbContext.Users.SingleAsync(u => u.UserId == user.UserId));
        Assert.Equal(newDateOfBirth, user.DateOfBirth);
        Assert.Equal(Clock.UtcNow, user.Updated);

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                Assert.Equal(UserUpdatedEventSource.ChangedByUser, userUpdatedEvent.Source);
                Assert.Equal(UserUpdatedEventChanges.DateOfBirth, userUpdatedEvent.Changes);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            });

        var redirectedResponse = await response.FollowRedirect(HttpClient);
        var redirectedDoc = await redirectedResponse.GetDocument();
        AssertEx.HtmlDocumentHasFlashSuccess(redirectedDoc, "Your date of birth has been updated");
    }

    private static string UrlEncode(string value) => UrlEncoder.Default.Encode(value);
}
