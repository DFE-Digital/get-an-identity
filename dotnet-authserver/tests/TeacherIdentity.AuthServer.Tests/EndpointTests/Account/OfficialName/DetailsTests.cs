using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.OfficialName;

public class DetailsTests : TestBase
{
    public DetailsTests(HostFixture hostFixture)
        : base(hostFixture)
    {
        HostFixture.SetUserId(TestUsers.DefaultUserWithTrn.UserId);
        MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasPendingNameChange: false);
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

        var request = new HttpRequestMessage(HttpMethod.Get, AppendQueryParameterSignature($"/account/official-name/details"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, AppendQueryParameterSignature($"/account/official-name/details"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequestWithNamesInQueryParam_PopulatesFieldsFromQueryParams()
    {
        // Arrange
        var previouslyStatedFirstName = Faker.Name.First();
        var previouslyStatedMiddleName = Faker.Name.Middle();
        var previouslyStatedLastName = Faker.Name.Last();

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature(
                $"/account/official-name/details" +
                $"?firstName={Uri.EscapeDataString(previouslyStatedFirstName)}" +
                $"&middleName={Uri.EscapeDataString(previouslyStatedMiddleName)}" +
                $"&lastName={Uri.EscapeDataString(previouslyStatedLastName)}"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();
        Assert.Equal(previouslyStatedFirstName, doc.GetElementById("FirstName")?.GetAttribute("value"));
        Assert.Equal(previouslyStatedMiddleName, doc.GetElementById("MiddleName")?.GetAttribute("value"));
        Assert.Equal(previouslyStatedLastName, doc.GetElementById("LastName")?.GetAttribute("value"));
    }

    [Theory]
    [MemberData(nameof(InvalidNamesData))]
    public async Task Post_InvalidNames_ReturnsBadRequest(
        string firstName,
        string middleName,
        string lastName,
        string errorField,
        string errorMessage)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, AppendQueryParameterSignature($"/account/official-name/details"))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", firstName },
                { "MiddleName", middleName },
                { "LastName", lastName },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, errorField, errorMessage);
    }

    [Fact]
    public async Task Post_NamesUnchanged_ReturnsBadRequest()
    {
        // Arrange
        var user = TestUsers.DefaultUserWithTrn;
        var middleName = Faker.Name.Middle();
        HostFixture.SetUserId(user.UserId);
        MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasPendingNameChange: false, middleName);

        var request = new HttpRequestMessage(HttpMethod.Post, AppendQueryParameterSignature($"/account/official-name/details"))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", user.FirstName },
                { "MiddleName", middleName },
                { "LastName", user.LastName },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "FirstName", "The name entered matches your official name");
    }

    [Fact]
    public async Task Post_MiddleNameFromEmptyToNullIsNamesUnchanged_ReturnsBadRequest()
    {
        // Arrange
        var user = TestUsers.DefaultUserWithTrn;
        var middleName = Faker.Name.Middle();
        HostFixture.SetUserId(user.UserId);
        MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasPendingNameChange: false, string.Empty);

        var request = new HttpRequestMessage(HttpMethod.Post, AppendQueryParameterSignature($"/account/official-name/details"))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", user.FirstName },
                { "LastName", user.LastName },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "FirstName", "The name entered matches your official name");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ValidForm_RedirectsToAppropriatePage(bool fromConfirmPage)
    {
        // Arrange
        var clientRedirectInfo = CreateClientRedirectInfo();

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/official-name/details?{clientRedirectInfo.ToQueryParam()}&fileName=myfile&fileId=12345&fromConfirmPage={fromConfirmPage}"))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", Faker.Name.First() },
                { "MiddleName", Faker.Name.Middle() },
                { "LastName", Faker.Name.Last() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        if (fromConfirmPage)
        {
            Assert.StartsWith($"/account/official-name/confirm", response.Headers.Location?.OriginalString);
        }
        else
        {
            Assert.StartsWith($"/account/official-name/evidence", response.Headers.Location?.OriginalString);
        }

        Assert.Contains(clientRedirectInfo.ToQueryParam(), response.Headers.Location?.OriginalString);
    }

    private void MockDqtApiResponse(User user, bool hasPendingNameChange, string middleName = "")
    {
        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthServer.Services.DqtApi.TeacherInfo()
            {
                DateOfBirth = user.DateOfBirth!.Value,
                FirstName = user.FirstName,
                MiddleName = middleName,
                LastName = user.LastName,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingDateOfBirthChange = false,
                PendingNameChange = hasPendingNameChange,
                Email = null
            });
    }

    public static TheoryData<string, string, string, string, string> InvalidNamesData { get; } = new()
    {
        {
            "Joe",
            "",
            "",
            "LastName",
            "Enter your last name"
        },
        {
            "",
            "",
            "Bloggs",
            "FirstName",
            "Enter your first name"
        },
        {
            new string('x', 101),
            "",
            "Bloggs",
            "FirstName",
            "First name must be 100 characters or less"
        },
        {
            "Joe",
            "",
            new string('x', 101),
            "LastName",
            "Last name must be 100 characters or less"
        },
        {
            "Joe",
            new string('x', 101),
            "Bloggs",
            "MiddleName",
            "Middle name must be 100 characters or less"
        }
    };
}
