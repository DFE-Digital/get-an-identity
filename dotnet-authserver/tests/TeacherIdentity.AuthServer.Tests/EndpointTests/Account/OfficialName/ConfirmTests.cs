using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.OfficialName;

public class ConfirmTests : TestBase
{
    private readonly string _validRequestUrl;
    private readonly ClientRedirectInfo _clientRedirectInfo;

    public ConfirmTests(HostFixture hostFixture)
        : base(hostFixture)
    {
        var user = TestUsers.DefaultUserWithTrn;

        HostFixture.SetUserId(user.UserId);
        MockDqtApiResponse(user, hasPendingNameChange: false);

        _clientRedirectInfo = CreateClientRedirectInfo();
        var guid = Guid.NewGuid();

        _validRequestUrl =
            AppendQueryParameterSignature(
                $"/account/official-name/confirm" +
                $"?{_clientRedirectInfo.ToQueryParam()}" +
                $"&firstName={Faker.Name.First()}" +
                $"&middleName={Faker.Name.Middle()}" +
                $"&lastName={Faker.Name.Last()}" +
                $"&fileId={guid}" +
                $"&fileName={user.UserId}/{guid}" +
                $"&preferredName={user.PreferredName}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Get_OfficialNameChangeDisabled_ReturnsBadRequest(bool hasTrn)
    {
        // Arrange
        if (hasTrn)
        {
            HostFixture.SetUserId(TestUsers.DefaultUserWithTrn.UserId);
            MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasPendingNameChange: true);
        }
        else
        {
            HostFixture.SetUserId(TestUsers.DefaultUser.UserId);
        }

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            _validRequestUrl);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Theory]
    [InlineData("firstName")]
    [InlineData("lastName")]
    [InlineData("fileName")]
    [InlineData("fileId")]
    [InlineData("preferredName")]
    public async Task Get_MissingQueryParameter_ReturnsBadRequest(string missingQueryParameter)
    {
        // Arrange
        var guid = Guid.NewGuid();
        var requestUrl =
            $"/account/official-name/confirm" +
                $"?{_clientRedirectInfo.ToQueryParam()}" +
                $"&firstName={Faker.Name.First()}" +
                $"&middleName={Faker.Name.Middle()}" +
                $"&lastName={Faker.Name.Last()}" +
                $"&fileId={guid}" +
                $"&fileName=MyFile" +
                $"&preferredName={Faker.Name.FullName()}";
        var invalidRequestUrl = new Regex($@"[\?&]{missingQueryParameter}=[^&]*").Replace(requestUrl, string.Empty);

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature(invalidRequestUrl));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, _validRequestUrl);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Post_ValidForm_UpdatesUserAsAppropriateAndRedirectsToAccountPage(bool changedPreferredName)
    {
        // Arrange
        var user = await TestData.CreateUser(
            hasTrn: true,
            userType: UserType.Default,
            hasPreferredName: true);
        HostFixture.SetUserId(user.UserId);
        MockDqtApiResponse(user, hasPendingNameChange: false);

        var guid = Guid.NewGuid();
        var newPreferredName = changedPreferredName ? Faker.Name.FullName() : user.PreferredName;

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature(
                $"/account/official-name/confirm" +
                $"?{_clientRedirectInfo.ToQueryParam()}" +
                $"&firstName={Faker.Name.First()}" +
                $"&middleName={Faker.Name.Middle()}" +
                $"&lastName={Faker.Name.Last()}" +
                $"&fileId={guid}" +
                $"&fileName={user.UserId}/{guid}" +
                $"&preferredName={newPreferredName}"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/account?{_clientRedirectInfo.ToQueryParam()}", response.Headers.Location?.OriginalString);

        var updatedUser = await TestData.WithDbContext(dbContext => dbContext.Users.SingleAsync(u => u.UserId == user.UserId));
        if (changedPreferredName)
        {
            Assert.Equal(Clock.UtcNow, updatedUser.Updated);
            Assert.Equal(newPreferredName, updatedUser.PreferredName);

            EventObserver.AssertEventsSaved(
                e =>
                {
                    var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                    Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                    Assert.Equal(UserUpdatedEventSource.ChangedByUser, userUpdatedEvent.Source);
                    Assert.Equal(UserUpdatedEventChanges.PreferredName, userUpdatedEvent.Changes);
                    Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
                });
        }
        else
        {
            Assert.Equal(user.PreferredName, updatedUser.PreferredName);
            EventObserver.AssertEventsSaved();
        }

        HostFixture.DqtEvidenceStorageService.Verify(s => s.GetSasConnectionString(It.IsAny<string>(), It.IsAny<int>()));
        HostFixture.DqtApiClient.Verify(s => s.PostTeacherNameChange(It.IsAny<TeacherNameChangeRequest>(), It.IsAny<CancellationToken>()));

        var redirectedResponse = await response.FollowRedirect(HttpClient);
        var redirectedDoc = await redirectedResponse.GetDocument();
        AssertEx.HtmlDocumentHasFlashSuccess(redirectedDoc, "We’ve received your request to change your official name");
    }

    private void MockDqtApiResponse(User user, bool hasPendingNameChange)
    {
        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = user.DateOfBirth!.Value,
                FirstName = user.FirstName,
                MiddleName = "",
                LastName = user.LastName,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingDateOfBirthChange = false,
                PendingNameChange = hasPendingNameChange,
                Email = null,
                Alerts = Array.Empty<AlertInfo>()
            });
    }
}
