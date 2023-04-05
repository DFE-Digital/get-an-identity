using System.Text.RegularExpressions;
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
                $"/account/official-name/confirm?{_clientRedirectInfo.ToQueryParam()}&firstName={Faker.Name.First()}&middleName={Faker.Name.Middle()}&lastName={Faker.Name.Last()}&fileId={guid}&fileName={user.UserId}/{guid}");
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

        var request = new HttpRequestMessage(HttpMethod.Get, $"/account/official-name/confirm");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Theory]
    [InlineData("firstName")]
    [InlineData("lastName")]
    [InlineData("fileId")]
    [InlineData("fileName")]
    public async Task Get_MissingQueryParameter_ReturnsBadRequest(string missingQueryParameter)
    {
        // Arrange
        var guid = Guid.NewGuid();
        var requestUrl = $"/account/official-name/confirm?{_clientRedirectInfo.ToQueryParam()}&firstName={Faker.Name.First()}&middleName={Faker.Name.Middle()}&lastName={Faker.Name.Last()}&fileId={guid}&fileName=1/{guid}";
        var invalidRequestUrl = new Regex($@"[\?&]{missingQueryParameter}=[^&]*").Replace(requestUrl, "");

        var request = new HttpRequestMessage(HttpMethod.Get, AppendQueryParameterSignature(invalidRequestUrl));

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

    [Fact]
    public async Task Post_ValidForm_RedirectsToAccountPage()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _validRequestUrl);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/account?{_clientRedirectInfo.ToQueryParam()}", response.Headers.Location?.OriginalString);

        HostFixture.DqtEvidenceStorageService.Verify(s => s.GetSasConnectionString(It.IsAny<string>(), It.IsAny<int>()));
        HostFixture.DqtApiClient.Verify(s => s.PostTeacherNameChange(It.IsAny<TeacherNameChangeRequest>(), It.IsAny<CancellationToken>()));

        var redirectedResponse = await response.FollowRedirect(HttpClient);
        var redirectedDoc = await redirectedResponse.GetDocument();
        AssertEx.HtmlDocumentHasFlashSuccess(redirectedDoc, "We’ve received your request to change your official name");
    }

    private void MockDqtApiResponse(User user, bool hasPendingNameChange)
    {
        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthServer.Services.DqtApi.TeacherInfo()
            {
                DateOfBirth = user.DateOfBirth!.Value,
                FirstName = user.FirstName,
                LastName = user.LastName,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingNameChange = hasPendingNameChange
            });
    }
}
