using System.Text.Encodings.Web;
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

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/date-of-birth")
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
        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/date-of-birth")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "DateOfBirth", "Enter your date of birth");
    }

    [Fact]
    public async Task Post_FutureDateOfBirth_RedirectsToConfirmPage()
    {
        // Arrange
        var dateOfBirth = new DateOnly(2100, 1, 1);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/date-of-birth")
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
    public async Task Post_ValidDateOfBirth_RedirectsToConfirmPage()
    {
        // Arrange
        var dateOfBirth = new DateOnly(2000, 1, 1);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/date-of-birth")
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
    public async Task Post_ValidDateOfBirth_RedirectsToConfirmPageWithCorrectReturnUrl()
    {
        // Arrange
        var dateOfBirth = new DateOnly(2000, 1, 1);

        var client = TestClients.Client1;
        var redirectUri = client.RedirectUris.First().GetLeftPart(UriPartial.Authority);

        var returnUrl = UrlEncoder.Default.Encode($"/account?client_id={client.ClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/date-of-birth?returnUrl={returnUrl}")
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
        Assert.Contains($"returnUrl={returnUrl}", response.Headers.Location?.OriginalString);
    }

    private void MockDqtApiResponse(User user, bool hasDobConflict, bool hasPendingDobChange)
    {
        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthServer.Services.DqtApi.TeacherInfo()
            {
                DateOfBirth = hasDobConflict ? user.DateOfBirth!.Value.AddDays(1) : user.DateOfBirth!.Value,
                FirstName = user.FirstName,
                LastName = user.LastName,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingDateOfBirthChange = hasPendingDobChange
            });
    }
}
