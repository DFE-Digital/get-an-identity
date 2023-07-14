using TeacherIdentity.AuthServer.Models;
namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.DateOfBirth;

public class DateOfBirthTests : TestBase
{
    public DateOfBirthTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Post_DateOfBirthChangeDisabled_ReturnsBadRequest(bool hasDobConflict, bool hasPendingDobChange)
    {
        // Arrange
        HostFixture.SetUserId(TestUsers.DefaultUserWithTrn.UserId);
        MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasDobConflict, hasPendingDobChange);

        var dateOfBirth = new DateOnly(2000, 1, 1);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/date-of-birth"))
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
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyDateOfBirth_ReturnsError()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/date-of-birth"))
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
    public async Task Post_InvalidDateOfBirth_ReturnsError(string dateOfBirthString, string errorMessage)
    {
        // Arrange
        var dateOfBirth = DateOnly.Parse(dateOfBirthString);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/date-of-birth"))
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
    public async Task Post_ValidDateOfBirth_RedirectsToConfirmPage()
    {
        // Arrange
        var dateOfBirth = new DateOnly(2000, 1, 1);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/date-of-birth"))
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
        Assert.StartsWith("/account/date-of-birth/confirm", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_ValidDateOfBirth_RedirectsToConfirmPageWithClientRedirectInfo()
    {
        // Arrange
        var dateOfBirth = new DateOnly(2000, 1, 1);

        var clientRedirectInfo = CreateClientRedirectInfo();

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/date-of-birth?{clientRedirectInfo.ToQueryParam()}"))
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
        Assert.Contains(clientRedirectInfo.ToQueryParam(), response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequestWithNamesInQueryParam_PopulatesFieldsFromQueryParam()
    {
        // Arrange
        var previouslyStatedDateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature($"/account/date-of-birth?dateOfBirth={previouslyStatedDateOfBirth:yyyy-MM-dd}"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();
        Assert.Equal($"{previouslyStatedDateOfBirth:%d}", doc.GetElementById("DateOfBirth.Day")?.GetAttribute("value"));
        Assert.Equal($"{previouslyStatedDateOfBirth:%M}", doc.GetElementById("DateOfBirth.Month")?.GetAttribute("value"));
        Assert.Equal($"{previouslyStatedDateOfBirth:yyyy}", doc.GetElementById("DateOfBirth.Year")?.GetAttribute("value"));
    }

    [Fact]
    public async Task Get_ValidRequestWithoutNamesInQueryParam_PopulatesFieldsFromDatabase()
    {
        // Arrange
        var defaultDateOfBirth = TestUsers.DefaultUser.DateOfBirth!.Value;
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature($"/account/date-of-birth"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();
        Assert.Equal($"{defaultDateOfBirth:%d}", doc.GetElementById("DateOfBirth.Day")?.GetAttribute("value"));
        Assert.Equal($"{defaultDateOfBirth:%M}", doc.GetElementById("DateOfBirth.Month")?.GetAttribute("value"));
        Assert.Equal($"{defaultDateOfBirth:yyyy}", doc.GetElementById("DateOfBirth.Year")?.GetAttribute("value"));
    }

    private void MockDqtApiResponse(User user, bool hasDobConflict, bool hasPendingDobChange)
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
                PendingNameChange = false,
                PendingDateOfBirthChange = hasPendingDobChange,
                Email = null
            });
    }
}
