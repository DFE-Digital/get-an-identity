using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.OfficialDateOfBirth;

public class EvidenceTests : TestBase
{
    private readonly string _validRequestUrl;
    private readonly ClientRedirectInfo _clientRedirectInfo;

    public EvidenceTests(HostFixture hostFixture)
        : base(hostFixture)
    {
        HostFixture.SetUserId(TestUsers.DefaultUserWithTrn.UserId);
        MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasDobConflict: true, hasPendingDateOfBirthChange: false);
        MockSuccessfulDqtEvidenceFileUpload();

        _clientRedirectInfo = CreateClientRedirectInfo();

        var dateOfBirth = new DateOnly(2000, 1, 1);

        _validRequestUrl =
            AppendQueryParameterSignature($"/account/official-date-of-birth/evidence?{_clientRedirectInfo.ToQueryParam()}&dateOfBirth={dateOfBirth.ToString("yyyy-MM-dd")}");
    }

    [Theory]
    [MemberData(nameof(InvalidDateOfBirthState))]
    public async Task Get_OfficialDateOfBirthChangeDisabled_ReturnsBadRequest(bool hasTrn, bool hasDobConflict, bool hasPendingDobChange)
    {
        // Arrange
        if (hasTrn)
        {
            HostFixture.SetUserId(TestUsers.DefaultUserWithTrn.UserId);
            MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasDobConflict: hasDobConflict, hasPendingDateOfBirthChange: hasPendingDobChange);
        }
        else
        {
            HostFixture.SetUserId(TestUsers.DefaultUser.UserId);
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"/account/official-date-of-birth");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_MissingDateOfBirthQueryParameter_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequestUrl = $"/account/official-date-of-birth/evidence?{_clientRedirectInfo.ToQueryParam()}";

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
        await AssertEx.HtmlResponseHasError(response, "EvidenceFile", "The selected file must be an image or a PDF");
    }

    [Theory]
    [InlineData("pdf")]
    [InlineData("jpg")]
    [InlineData("jpeg")]
    public async Task Post_ValidFileTypeUploaded_UploadsEvidenceToBlobStorageAndRedirects(string fileType)
    {
        // Arrange
        var user = TestUsers.DefaultUserWithTrn;
        HostFixture.SetUserId(user.UserId);

        MockDqtApiResponse(user, hasDobConflict: true, hasPendingDateOfBirthChange: false);

        var file = CreateFormFileUpload(fileType);

        var request = new HttpRequestMessage(HttpMethod.Post, _validRequestUrl)
        {
            Content = file
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith($"/account/official-date-of-birth/confirm", response.Headers.Location?.OriginalString);

        Assert.Contains(_clientRedirectInfo.ToQueryParam(), response.Headers.Location?.OriginalString);
        Assert.Contains("fileName=", response.Headers.Location?.OriginalString);

        HostFixture.DqtEvidenceStorageService.Verify(s => s.TrySafeUpload(It.IsAny<IFormFile>(), It.Is<string>(arg => arg.StartsWith($"{user.UserId}/"))));
    }

    [Fact]
    public async Task Post_ValidFileTypeMalwareDetected_RendersError()
    {
        // Arrange
        HostFixture.DqtEvidenceStorageService
            .Setup(s => s.TrySafeUpload(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var user = TestUsers.DefaultUserWithTrn;
        HostFixture.SetUserId(user.UserId);

        MockDqtApiResponse(user, hasDobConflict: true, hasPendingDateOfBirthChange: false);

        var file = CreateFormFileUpload("jpg");

        var request = new HttpRequestMessage(HttpMethod.Post, _validRequestUrl)
        {
            Content = file
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "EvidenceFile", "The selected file contains a virus");

        HostFixture.DqtEvidenceStorageService.Verify(s => s.TrySafeUpload(It.IsAny<IFormFile>(), It.Is<string>(arg => arg.StartsWith($"{user.UserId}/"))));
    }

    private void MockDqtApiResponse(User user, bool hasDobConflict, bool hasPendingDateOfBirthChange)
    {
        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthServer.Services.DqtApi.TeacherInfo()
            {
                DateOfBirth = hasDobConflict ? user.DateOfBirth!.Value.AddDays(1) : user.DateOfBirth!.Value,
                FirstName = user.FirstName,
                MiddleName = "",
                LastName = user.LastName,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingDateOfBirthChange = hasPendingDateOfBirthChange,
                PendingNameChange = false,
                Email = null
            });
    }

    public static TheoryData<bool, bool, bool> InvalidDateOfBirthState { get; } = new()
    {
        // hasTrn, hasDobConflicts, hasPendingDobChange
        { false, false, false },
        { true, false, false },
        { true, true, true },
    };

    private MultipartFormDataContent CreateFormFileUpload(string fileType)
    {
        var byteArrayContent = new ByteArrayContent(new byte[] { });
        byteArrayContent.Headers.Add("Content-Type", "application/octet-stream");

        var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(byteArrayContent, "EvidenceFile", $"evidence.{fileType}");

        return multipartContent;
    }

    private void MockSuccessfulDqtEvidenceFileUpload()
    {
        HostFixture.DqtEvidenceStorageService
            .Setup(s => s.TrySafeUpload(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(true);
    }
}
