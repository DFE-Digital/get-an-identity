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

        var request = new HttpRequestMessage(HttpMethod.Get, AppendQueryParameterSignature($"/account/official-date-of-birth/details"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, AppendQueryParameterSignature($"/account/official-date-of-birth/details"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequestWithDateInQueryParam_PopulatesFieldFromQueryParam()
    {
        // Arrange
        var previouslyStatedDateOfBirth = TestUsers.DefaultUser.DateOfBirth!.Value.AddDays(1);

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature($"/account/official-date-of-birth/details?dateOfBirth={previouslyStatedDateOfBirth:yyyy-MM-dd}"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();
        Assert.Equal(previouslyStatedDateOfBirth.Day.ToString(), doc.GetElementById("DateOfBirth.Day")?.GetAttribute("value"));
        Assert.Equal(previouslyStatedDateOfBirth.Month.ToString(), doc.GetElementById("DateOfBirth.Month")?.GetAttribute("value"));
        Assert.Equal(previouslyStatedDateOfBirth.Year.ToString(), doc.GetElementById("DateOfBirth.Year")?.GetAttribute("value"));
    }

    [Fact]
    public async Task Post_EmptyDateOfBirth_ReturnsBadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, AppendQueryParameterSignature($"/account/official-date-of-birth/details"))
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "DateOfBirth", "Enter your date of birth");
    }

    [Theory]
    [InlineData("01/01/2100", "Your date of birth must be in the past")]
    [InlineData("01/01/1899", "Enter a valid date of birth")]
    public async Task Post_InvalidDateOfBirth_ReturnsBadRequest(string dateOfBirthString, string errorMessage)
    {
        // Arrange
        var dateOfBirth = DateOnly.Parse(dateOfBirthString);

        var request = new HttpRequestMessage(HttpMethod.Post, AppendQueryParameterSignature($"/account/official-date-of-birth/details"))
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
        await AssertEx.HtmlResponseHasError(response, "DateOfBirth", errorMessage);
    }

    [Fact]
    public async Task Post_DateOfBirthUnchanged_ReturnsBadRequest()
    {
        // Arrange
        var user = TestUsers.DefaultUserWithTrn;
        var dateOfBirth = user.DateOfBirth;

        HostFixture.SetUserId(user.UserId);

        MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasDobConflict: true, hasPendingDateOfBirthChange: false);

        var request = new HttpRequestMessage(HttpMethod.Post, AppendQueryParameterSignature($"/account/official-date-of-birth/details"))
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

        var previouslyStatedDateOfBirth = TestUsers.DefaultUser.DateOfBirth!.Value.AddDays(1);
        var dateOfBirth = new DateOnly(2000, 1, 1);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/official-date-of-birth/details?{clientRedirectInfo.ToQueryParam()}&dateOfBirth={previouslyStatedDateOfBirth:yyyy-MM-dd}"))
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
}
