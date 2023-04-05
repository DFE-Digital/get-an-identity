using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.OfficialName;

public class EvidenceTests : TestBase
{
    private readonly string _validRequestUrl;
    private readonly ClientRedirectInfo _clientRedirectInfo;

    public EvidenceTests(HostFixture hostFixture)
        : base(hostFixture)
    {
        HostFixture.SetUserId(TestUsers.DefaultUserWithTrn.UserId);
        MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasPendingNameChange: false);

        _clientRedirectInfo = CreateClientRedirectInfo();

        _validRequestUrl =
            AppendQueryParameterSignature($"/account/official-name/evidence?{_clientRedirectInfo.ToQueryParam()}&firstName={Faker.Name.First()}&middleName={Faker.Name.Middle()}&lastName={Faker.Name.Last()}");
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

        var request = new HttpRequestMessage(HttpMethod.Get, $"/account/official-name/evidence");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Theory]
    [InlineData("firstName")]
    [InlineData("lastName")]
    public async Task Get_MissingQueryParameter_ReturnsBadRequest(string missingQueryParameter)
    {
        // Arrange
        var requestUrl = $"/account/official-name/evidence?{_clientRedirectInfo.ToQueryParam()}&firstName={Faker.Name.First()}&middleName={Faker.Name.Middle()}&lastName={Faker.Name.Last()}";
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
        var request = new HttpRequestMessage(HttpMethod.Get, AppendQueryParameterSignature(_validRequestUrl));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_EmptyMiddleName_ReturnsSuccess()
    {
        // Arrange
        var firstName = Faker.Name.First();
        var middleName = "";
        var lastName = Faker.Name.Last();

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature($"/account/official-name/evidence?firstName={firstName}&middleName={middleName}&lastName={lastName}"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_NoFileUploaded_RendersError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _validRequestUrl);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "EvidenceFile", "Select a file");
    }

    [Fact]
    public async Task Post_InvalidFileTypeUploaded_RendersError()
    {
        // Arrange
        var file = CreateFormFileUpload("txt");

        var request = new HttpRequestMessage(HttpMethod.Post, _validRequestUrl)
        {
            Content = file
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "EvidenceFile", "The selected file must be a CSV or JPEG");
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("jpg")]
    [InlineData("jpeg")]
    public async Task Post_ValidFileTypeUploaded_UploadsEvidenceToBlobStorageAndRedirects(string fileType)
    {
        // Arrange
        var user = TestUsers.DefaultUserWithTrn;
        HostFixture.SetUserId(user.UserId);
        MockDqtApiResponse(user, hasPendingNameChange: false);

        var file = CreateFormFileUpload(fileType);

        var request = new HttpRequestMessage(HttpMethod.Post, _validRequestUrl)
        {
            Content = file
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith($"/account/official-name/confirm", response.Headers.Location?.OriginalString);

        Assert.Contains(_clientRedirectInfo.ToQueryParam(), response.Headers.Location?.OriginalString);
        Assert.Contains("fileId=", response.Headers.Location?.OriginalString);
        Assert.Contains("fileName=", response.Headers.Location?.OriginalString);

        HostFixture.DqtEvidenceStorageService.Verify(s => s.Upload(It.IsAny<IFormFile>(), It.Is<string>(arg => arg.StartsWith($"{user.UserId}/"))));
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

    private MultipartFormDataContent CreateFormFileUpload(string fileType)
    {
        var byteArrayContent = new ByteArrayContent(new byte[] { });
        byteArrayContent.Headers.Add("Content-Type", "application/octet-stream");

        var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(byteArrayContent, "EvidenceFile", $"evidence.{fileType}");

        return multipartContent;
    }

    private static string UrlEncode(string value) => UrlEncoder.Default.Encode(value);
}
