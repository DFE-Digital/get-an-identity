using AngleSharp.Html.Dom;
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

        var client = TestClients.DefaultClient;
        var redirectUri = client.RedirectUris.First().GetLeftPart(UriPartial.Authority);
        var signOutUri = client.RedirectUris.First().GetLeftPart(UriPartial.Authority) + "/sign-out";

        var request = new HttpRequestMessage(
        HttpMethod.Get,
            $"/account?client_id=not_a_real_client_id&redirect_uri={Uri.EscapeDataString(redirectUri)}&sign_out_uri={Uri.EscapeDataString(signOutUri)}");

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

        var client = TestClients.DefaultClient;
        var signOutUri = client.RedirectUris.First().GetLeftPart(UriPartial.Authority) + "/sign-out";

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/account?client_id={client.ClientId}&redirect_uri={Uri.EscapeDataString("https://google.com")}&sign_out_uri={Uri.EscapeDataString(signOutUri)}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_InvalidSignOutUriDomain_ReturnsBadRequest()
    {
        // Arrange
        var user = await TestData.CreateUser();
        HostFixture.SetUserId(user.UserId);

        var client = TestClients.DefaultClient;
        var redirectUri = client.RedirectUris.First().GetLeftPart(UriPartial.Authority);

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/account?client_id={client.ClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&sign_out_uri={Uri.EscapeDataString("https://google.com")}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_ValidRequestForUserWithoutTrn_ShowsIdentityNames(bool hasPreferredName)
    {
        // Arrange
        var user = await TestData.CreateUser(hasPreferredName: hasPreferredName);
        HostFixture.SetUserId(user.UserId);

        var request = new HttpRequestMessage(HttpMethod.Get, "/account");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.Equal(user.FirstName, doc.GetSummaryListValueForKey("First names"));
        Assert.Equal(user.MiddleName, doc.GetSummaryListValueForKey("Middle names"));
        Assert.Equal(user.LastName, doc.GetSummaryListValueForKey("Last names"));
        if (hasPreferredName)
        {
            Assert.Equal(user.PreferredName, doc.GetSummaryListValueForKey("Preferred name"));
        }
        else
        {
            Assert.Equal("Not provided", doc.GetSummaryListValueForKey("Preferred name"));
        }

        Assert.Equal($"{user.DateOfBirth?.ToString(Constants.DateFormat)}", doc.GetSummaryListValueForKey("Date of birth"));
        Assert.Equal(user.EmailAddress, doc.GetSummaryListValueForKey("Email"));
        Assert.Equal(user.MobileNumber, doc.GetSummaryListValueForKey("Mobile phone"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_ValidRequestForUserWithTrn_ShowsOfficialNames(bool hasPreferredName)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true, hasPreferredName: hasPreferredName);
        HostFixture.SetUserId(user.UserId);

        var officialFirstName = Faker.Name.First();
        var officialMiddleName = Faker.Name.Middle();
        var officialLastName = Faker.Name.Last();

        HostFixture.DqtApiClient
            .Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = user.DateOfBirth!.Value,
                FirstName = officialFirstName,
                MiddleName = officialMiddleName,
                LastName = officialLastName,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingNameChange = false,
                PendingDateOfBirthChange = false,
                Email = null,
                Alerts = Array.Empty<AlertInfo>(),
                AllowIdSignInWithProhibitions = false
            });

        var request = new HttpRequestMessage(HttpMethod.Get, "/account");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.Equal(officialFirstName, doc.GetSummaryListValueForKey("First names"));
        Assert.Equal(officialMiddleName, doc.GetSummaryListValueForKey("Middle names"));
        Assert.Equal(officialLastName, doc.GetSummaryListValueForKey("Last names"));
        if (hasPreferredName)
        {
            Assert.Equal(user.PreferredName, doc.GetSummaryListValueForKey("Preferred name"));
        }
        else
        {
            Assert.Equal("Not provided", doc.GetSummaryListValueForKey("Preferred name"));
        }

        Assert.Equal($"{user.DateOfBirth?.ToString(Constants.DateFormat)}", doc.GetSummaryListValueForKey("Date of birth"));
        Assert.Equal(user.EmailAddress, doc.GetSummaryListValueForKey("Email"));
        Assert.Equal(user.MobileNumber, doc.GetSummaryListValueForKey("Mobile phone"));
    }

    [Theory]
    [MemberData(nameof(DateOfBirthState))]
    public async Task Get_ValidRequestForUser_ShowsCorrectDobSummaryRowElements(
        bool hasTrn,
        bool hasDobConflict,
        bool hasPendingReview,
        DobAccountPageElements dobAccountPageElements)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: hasTrn);
        var userDateOfBirth = user.DateOfBirth!.Value;
        HostFixture.SetUserId(user.UserId);

        HostFixture.DqtApiClient
            .Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = hasDobConflict ? userDateOfBirth.AddDays(1) : userDateOfBirth,
                FirstName = Faker.Name.First(),
                MiddleName = "",
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingNameChange = false,
                PendingDateOfBirthChange = hasPendingReview,
                Email = null,
                Alerts = Array.Empty<AlertInfo>(),
                AllowIdSignInWithProhibitions = false
            });

        var request = new HttpRequestMessage(HttpMethod.Get, "/account");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();
        AssertValidDobState(dobAccountPageElements, doc);
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
                MiddleName = "",
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingNameChange = true,
                PendingDateOfBirthChange = false,
                Email = null,
                Alerts = Array.Empty<AlertInfo>(),
                AllowIdSignInWithProhibitions = false
            });

        var request = new HttpRequestMessage(HttpMethod.Get, "/account");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.NotNull(doc.GetElementByTestId("first-name-pending-review-tag"));
        Assert.NotNull(doc.GetElementByTestId("middle-name-pending-review-tag"));
        Assert.NotNull(doc.GetElementByTestId("last-name-pending-review-tag"));
    }

    [Fact]
    public async Task Get_ValidRequestWithClientIdRedirectUriAndSignOutUri_RendersBackLinks()
    {
        // Arrange
        var user = await TestData.CreateUser();
        HostFixture.SetUserId(user.UserId);

        var client = TestClients.DefaultClient;
        var redirectUri = client.RedirectUris.First().GetLeftPart(UriPartial.Authority);
        var signOutUri = client.RedirectUris.First().GetLeftPart(UriPartial.Authority) + "/sign-out";

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/account?client_id={client.ClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&sign_out_uri={Uri.EscapeDataString(signOutUri)}");

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


    public static TheoryData<bool, bool, bool, DobAccountPageElements> DateOfBirthState { get; } = new()
    {
        // hasTrn, hasDobConflicts, hasPendingReview
        { false, false, false, DobAccountPageElements.ChangeLink },
        { true, false, false, DobAccountPageElements.None },
        { true, true, false, DobAccountPageElements.DqtSummaryRow | DobAccountPageElements.HintText | DobAccountPageElements.ChangeLink | DobAccountPageElements.NotificationBanner },
        { true, true, true, DobAccountPageElements.DqtSummaryRow | DobAccountPageElements.HintText | DobAccountPageElements.PendingReviewTag },
    };

    [Flags]
    public enum DobAccountPageElements
    {
        None = 0,
        HintText = 1 << 0,
        PendingReviewTag = 1 << 1,
        DqtSummaryRow = 1 << 2,
        ChangeLink = 1 << 3,
        NotificationBanner = 1 << 4
    }

    private void AssertValidDobState(DobAccountPageElements dobElements, IHtmlDocument doc)
    {
        Assert.Equal(dobElements.HasFlag(DobAccountPageElements.NotificationBanner), doc.GetElementByTestId("dob-conflict-notification-banner") != null);

        Assert.Equal(dobElements.HasFlag(DobAccountPageElements.PendingReviewTag), doc.GetElementByTestId("dob-pending-review-tag") != null);
        Assert.Equal(dobElements.HasFlag(DobAccountPageElements.ChangeLink), doc.GetElementByTestId("dob-change-link") != null);
        Assert.Equal(dobElements.HasFlag(DobAccountPageElements.HintText), doc.GetElementByTestId("dob-hint-text") != null);

        Assert.Equal(dobElements.HasFlag(DobAccountPageElements.DqtSummaryRow), doc.GetElementByTestId("dqt-dob-hint-text") != null);
        Assert.Equal(dobElements.HasFlag(DobAccountPageElements.ChangeLink) && dobElements.HasFlag(DobAccountPageElements.DqtSummaryRow), doc.GetElementByTestId("dqt-dob-change-link") != null);
        Assert.Equal(dobElements.HasFlag(DobAccountPageElements.PendingReviewTag) && dobElements.HasFlag(DobAccountPageElements.DqtSummaryRow), doc.GetElementByTestId("dqt-dob-pending-review-tag") != null);
    }
}
