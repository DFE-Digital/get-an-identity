using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.OfficialDateOfBirth;

public class DetailsTests : TestBase
{
    public DetailsTests(HostFixture hostFixture)
        : base(hostFixture)
    {
        HostFixture.SetUserId(TestUsers.DefaultUserWithTrn.UserId);
        MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasDobConflict: true, hasPendingDateOfBirthChange: false);
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
    public async Task Get_ValidRequest_ReturnsSuccess()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/account/official-date-of-birth/details");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyDateOfBirth_ReturnsBadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/official-date-of-birth/details")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "DateOfBirth", "Enter your date of birth");
    }

    [Fact]
    public async Task Post_FutureDateOfBirth_ReturnsBadRequest()
    {
        // Arrange
        var dateOfBirth = new DateOnly(2100, 1, 1);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/official-date-of-birth/details")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "DateOfBirth.Day", dateOfBirth.Day.ToString() },
                { "DateOfBirth.Month", dateOfBirth.Month.ToString() },
                { "DateOfBirth.Year", dateOfBirth.Year.ToString() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "DateOfBirth", "Your date of birth must be in the past");
    }

    [Fact]
    public async Task Post_DateOfBirthUnchanged_ReturnsBadRequest()
    {
        // Arrange
        var user = TestUsers.DefaultUserWithTrn;
        var dateOfBirth = user.DateOfBirth;

        HostFixture.SetUserId(user.UserId);

        MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasDobConflict: true, hasPendingDateOfBirthChange: false);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/official-date-of-birth/details")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "DateOfBirth.Day", dateOfBirth!.Value.AddDays(1).Day.ToString() },
                { "DateOfBirth.Month", dateOfBirth.Value.Month.ToString() },
                { "DateOfBirth.Year", dateOfBirth.Value.Year.ToString() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "DateOfBirth", "The date entered matches your date of birth");
    }

    [Fact]
    public async Task Post_ValidForm_RedirectsToEvidencePage()
    {
        // Arrange
        var clientRedirectInfo = CreateClientRedirectInfo();

        var dateOfBirth = new DateOnly(2000, 1, 1);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/official-date-of-birth/details?{clientRedirectInfo.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "DateOfBirth.Day", dateOfBirth.Day.ToString() },
                { "DateOfBirth.Month", dateOfBirth.Month.ToString() },
                { "DateOfBirth.Year", dateOfBirth.Year.ToString() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith($"/account/official-date-of-birth/evidence", response.Headers.Location?.OriginalString);
        Assert.Contains(clientRedirectInfo.ToQueryParam(), response.Headers.Location?.OriginalString);
    }

    private void MockDqtApiResponse(User user, bool hasDobConflict, bool hasPendingDateOfBirthChange)
    {
        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthServer.Services.DqtApi.TeacherInfo()
            {
                DateOfBirth = hasDobConflict ? user.DateOfBirth!.Value.AddDays(1) : user.DateOfBirth!.Value,
                FirstName = user.FirstName,
                LastName = user.LastName,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingDateOfBirthChange = hasPendingDateOfBirthChange
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
