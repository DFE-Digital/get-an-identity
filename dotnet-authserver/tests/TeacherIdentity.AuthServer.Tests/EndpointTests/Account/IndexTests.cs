using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account;

public class IndexTests : TestBase
{
    public IndexTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnknownClientId_ReturnsBadRequest()
    {
        // Arrange
        var user = await TestData.CreateUser();
        HostFixture.SetUserId(user.UserId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/account?client_id=not_a_real_client_id&redirect_uri={{Uri.EscapeDataString(\"https://google.com\")");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_InvalidRedirectUriDomain_ReturnsBadRequest()
    {
        // Arrange
        var user = await TestData.CreateUser();
        HostFixture.SetUserId(user.UserId);

        var client = TestClients.Client1;
        var request = new HttpRequestMessage(HttpMethod.Get, $"/account?client_id={client.ClientId}&redirect_uri={Uri.EscapeDataString("https://google.com")}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsUserDetails()
    {
        // Arrange
        var user = await TestData.CreateUser();
        HostFixture.SetUserId(user.UserId);

        var request = new HttpRequestMessage(HttpMethod.Get, "/account");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.Equal($"{user.FirstName} {user.LastName}", doc.GetSummaryListValueForKey("Name"));
        Assert.Equal($"{user.DateOfBirth:dd MMMM yyyy}", doc.GetSummaryListValueForKey("Date of birth"));
        Assert.Equal(user.EmailAddress, doc.GetSummaryListValueForKey("Email"));
        Assert.Equal(user.MobileNumber, doc.GetSummaryListValueForKey("Mobile number"));
    }

    [Fact]
    public async Task Get_ValidRequestForUserWithoutTrn_DoesNotShowOfficialNameRow()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: false);
        HostFixture.SetUserId(user.UserId);

        var request = new HttpRequestMessage(HttpMethod.Get, "/account");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.Null(doc.GetSummaryListRowForKey("Official name"));
    }

    [Fact]
    public async Task Get_ValidRequestForUserWithTrn_DoesShowOfficialNameRow()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        HostFixture.SetUserId(user.UserId);

        var officialFirstName = Faker.Name.First();
        var officialLastName = Faker.Name.Last();

        HostFixture.DqtApiClient
            .Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = user.DateOfBirth!.Value,
                FirstName = officialFirstName,
                LastName = officialLastName,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!
            });

        var request = new HttpRequestMessage(HttpMethod.Get, "/account");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.Equal(
            $"{officialFirstName} {officialLastName}",
            doc.GetSummaryListValueForKey("Official name")?.Replace("Displayed on teaching certificates", "").Trim());
    }

    [Fact]
    public async Task Get_ValidRequestForUserWithoutTrn_DoesNotShowTRNRow()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: false);
        HostFixture.SetUserId(user.UserId);

        var request = new HttpRequestMessage(HttpMethod.Get, "/account");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.Null(doc.GetSummaryListRowForKey("TRN"));
    }

    [Fact]
    public async Task Get_ValidRequestForUserWithTrn_DoesShowTRNRow()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        HostFixture.SetUserId(user.UserId);

        HostFixture.DqtApiClient
            .Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = user.DateOfBirth!.Value,
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!
            });

        var request = new HttpRequestMessage(HttpMethod.Get, "/account");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.Equal(user.Trn, doc.GetSummaryListValueForKey("TRN"));
    }

    [Fact]
    public async Task Get_ValidRequestForUserWithoutDobConflict_DoesNotShowNotificationBanner()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        HostFixture.SetUserId(user.UserId);

        HostFixture.DqtApiClient
            .Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = user.DateOfBirth!.Value,
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!
            });

        var request = new HttpRequestMessage(HttpMethod.Get, "/account");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.Null(doc.GetElementByTestId("dob-conflict-notification-banner"));
        Assert.Equal(1, doc.GetSummaryListRowCountForKey("Date of birth"));
    }

    [Fact]
    public async Task Get_ValidRequestForUserWithDobConflict_DoesShowNotificationBannerAndDqtDobRow()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        HostFixture.SetUserId(user.UserId);

        HostFixture.DqtApiClient
            .Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = user.DateOfBirth!.Value.AddDays(1),
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!
            });

        var request = new HttpRequestMessage(HttpMethod.Get, "/account");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.NotNull(doc.GetElementByTestId("dob-conflict-notification-banner"));
        Assert.Equal(2, doc.GetSummaryListRowCountForKey("Date of birth"));
    }

    [Fact]
    public async Task Get_ValidRequestForUserWithPendingDobChange_DoesShowPendingTagHidesBanner()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        HostFixture.SetUserId(user.UserId);

        HostFixture.DqtApiClient
            .Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = user.DateOfBirth!.Value.AddDays(1),
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingDateOfBirthChange = true,
            });

        var request = new HttpRequestMessage(HttpMethod.Get, "/account");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.Null(doc.GetElementByTestId("dob-conflict-notification-banner"));
        Assert.NotNull(doc.GetElementByTestId("dob-pending-review-tag"));
        Assert.Equal(2, doc.GetSummaryListRowCountForKey("Date of birth"));
    }

    [Fact]
    public async Task Get_ValidRequestForUserWithPendingNameChange_DoesShowPendingTag()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        HostFixture.SetUserId(user.UserId);

        HostFixture.DqtApiClient
            .Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = user.DateOfBirth!.Value,
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingNameChange = true,
            });

        var request = new HttpRequestMessage(HttpMethod.Get, "/account");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.NotNull(doc.GetElementByTestId("name-pending-review-tag"));
    }

    [Fact]
    public async Task Get_ValidRequestWithClientIdAndRedirectUri_RendersBackLinks()
    {
        // Arrange
        var user = await TestData.CreateUser();
        HostFixture.SetUserId(user.UserId);

        var client = TestClients.Client1;
        var redirectUri = client.RedirectUris.First().GetLeftPart(UriPartial.Authority);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/account?client_id={client.ClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.NotNull(doc.GetElementByTestId("BackLink"));
        Assert.NotNull(doc.GetElementByTestId("BackButton"));
    }

    [Fact]
    public async Task Get_ValidRequestWithoutRedirectUri_DoesNotRenderBackLinks()
    {
        // Arrange
        var user = await TestData.CreateUser();
        HostFixture.SetUserId(user.UserId);

        var request = new HttpRequestMessage(HttpMethod.Get, "/account");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.Null(doc.GetElementByTestId("BackLink"));
        Assert.Null(doc.GetElementByTestId("BackButton"));
    }
}
