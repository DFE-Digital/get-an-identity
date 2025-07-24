using System.Text.RegularExpressions;
using TeacherIdentity.AuthServer.Services.DqtApi;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.OfficialDateOfBirth;

public class ConfirmTests : TestBase
{
    private readonly string _validRequestUrl;
    private readonly ClientRedirectInfo _clientRedirectInfo;

    public ConfirmTests(HostFixture hostFixture)
        : base(hostFixture)
    {
        var user = TestUsers.DefaultUserWithTrn;

        HostFixture.SetUserId(user.UserId);
        MockDqtApiResponse(user, hasDobConflict: true, hasPendingDateOfBirthChange: false);

        _clientRedirectInfo = CreateClientRedirectInfo();
        var guid = Guid.NewGuid();

        var dateOfBirth = new DateOnly(2000, 1, 1);

        _validRequestUrl =
            AppendQueryParameterSignature(
                $"/account/official-date-of-birth/confirm?{_clientRedirectInfo.ToQueryParam()}&dateOfBirth={dateOfBirth.ToString("yyyy-MM-dd")}&fileId={guid}&fileName={user.UserId}/{guid}");
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

    [Theory]
    [InlineData("dateOfBirth")]
    [InlineData("fileName")]
    [InlineData("fileId")]
    public async Task Get_MissingQueryParameter_ReturnsBadRequest(string missingQueryParameter)
    {
        // Arrange
        var guid = Guid.NewGuid();
        var dateOfBirth = new DateOnly(2000, 1, 1);
        var requestUrl = $"/account/official-date-of-birth/confirm?{_clientRedirectInfo.ToQueryParam()}&dateOfBirth={dateOfBirth.ToString("yyyy-MM-dd")}&fileName=1/{guid}";
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
        HostFixture.DqtApiClient.Verify(s => s.PostTeacherDateOfBirthChange(It.IsAny<string>(), It.IsAny<TeacherDateOfBirthChangeRequest>(), It.IsAny<CancellationToken>()));

        var redirectedResponse = await response.FollowRedirect(HttpClient);
        var redirectedDoc = await redirectedResponse.GetDocument();
        AssertEx.HtmlDocumentHasFlashSuccess(redirectedDoc, "We’ve received your request to change your date of birth");
    }

    private void MockDqtApiResponse(User user, bool hasDobConflict, bool hasPendingDateOfBirthChange)
    {
        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = hasDobConflict ? user.DateOfBirth!.Value.AddDays(1) : user.DateOfBirth!.Value,
                FirstName = user.FirstName,
                MiddleName = "",
                LastName = user.LastName,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingDateOfBirthChange = hasPendingDateOfBirthChange,
                PendingNameChange = false,
                Email = null,
                Alerts = Array.Empty<AlertInfo>(),
                AllowIdSignInWithProhibitions = false
            });
    }

    public static TheoryData<bool, bool, bool> InvalidDateOfBirthState { get; } = new()
    {
        // hasTrn, hasDobConflicts, hasPendingDobChange
        { false, false, false },
        { true, false, false },
        { true, true, true },
    };
}
