
namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class UserTests : TestBase
{
    public UserTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/users/{user.UserId}");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/users/{user.UserId}");
    }

    [Fact]
    public async Task Get_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{userId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_UserIsStaffUsers_RedirectsToEditStaffUserPage()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Staff);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/admin/staff/{user.UserId}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequestForUserWithoutTrn_RendersExpectedContent()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: false, userType: Models.UserType.Default, hasPreferredName: true);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}");
        var formattedDateOfBirth = user.DateOfBirth?.ToString("d MMMM yyyy");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal($"{user.FirstName} {user.MiddleName} {user.LastName}", doc.GetElementByTestId("Name")?.TextContent);
        Assert.Equal($"{user.PreferredName}", doc.GetSummaryListValueForKey("Preferred name"));
        Assert.Equal(user.EmailAddress, doc.GetSummaryListValueForKey("Email address"));
        Assert.Equal(formattedDateOfBirth, doc.GetSummaryListValueForKey("Date of birth"));
        Assert.Equal("No", doc.GetSummaryListValueForKey("DQT record"));
        Assert.Equal("None", doc.GetSummaryListValueForKey("Merged user IDs"));
        Assert.NotEmpty(doc.GetSummaryListActionsForKey("DQT record"));
        Assert.Null(doc.GetElementByTestId("DqtSection"));
    }

    [Fact]
    public async Task Get_ValidRequestForUserWithTrn_RendersExpectedContent()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true, userType: Models.UserType.Default);

        var dqtFirstName = Faker.Name.First();
        var dqtLastName = Faker.Name.Last();
        var dqtDateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var dqtNino = Faker.Identification.UkNationalInsuranceNumber();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthServer.Services.DqtApi.TeacherInfo()
            {
                DateOfBirth = dqtDateOfBirth,
                FirstName = dqtFirstName,
                MiddleName = "",
                LastName = dqtLastName,
                NationalInsuranceNumber = dqtNino,
                Trn = user.Trn!,
                PendingNameChange = false,
                PendingDateOfBirthChange = false,
                Email = null
            });

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Empty(doc.GetSummaryListActionsForKey("DQT record"));
        Assert.NotNull(doc.GetElementByTestId("DqtSection"));
        Assert.Equal($"{dqtFirstName} {dqtLastName}", doc.GetSummaryListValueForKey("DQT name"));
        Assert.Equal(dqtDateOfBirth.ToString("d MMMM yyyy"), doc.GetElementByTestId("DqtSection")?.GetSummaryListValueForKey("Date of birth"));
        Assert.Equal("Provided", doc.GetSummaryListValueForKey("National insurance number"));
        Assert.Equal(user.Trn, doc.GetSummaryListValueForKey("TRN"));
    }

    [Fact]
    public async Task Get_ValidRequestForUserWithMergedUsers_RendersExpectedContent()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: false, userType: Models.UserType.Default);
        var mergedUser = await TestData.CreateUser(hasTrn: false, userType: Models.UserType.Default, mergedWithUserId: user.UserId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(mergedUser.UserId.ToString(), doc.GetSummaryListValueForKey("Merged user IDs"));
    }

    [Theory]
    [InlineData(Models.TrnVerificationLevel.Low, "Low")]
    [InlineData(Models.TrnVerificationLevel.Medium, "Medium")]
    public async Task Get_ValidRequestForTrnVerificationLevel_RendersExpectedContent(Models.TrnVerificationLevel trnVerificationLevel, string verificationLevelString)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true, userType: Models.UserType.Teacher, hasPreferredName: true, trnVerificationLevel: trnVerificationLevel);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}");
        var dqtFirstName = Faker.Name.First();
        var dqtLastName = Faker.Name.Last();
        var dqtDateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var dqtNino = Faker.Identification.UkNationalInsuranceNumber();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthServer.Services.DqtApi.TeacherInfo()
            {
                DateOfBirth = dqtDateOfBirth,
                FirstName = dqtFirstName,
                MiddleName = "",
                LastName = dqtLastName,
                NationalInsuranceNumber = dqtNino,
                Trn = user.Trn!,
                PendingNameChange = false,
                PendingDateOfBirthChange = false,
                Email = null
            });

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(verificationLevelString, doc.GetSummaryListValueForKey("TRN verification level"));
    }

    [Fact]
    public async Task Get_ValidRequestForUserWithoutTRN_DoesNotRenderTrnVerificationLevelSummaryRow()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: false, userType: Models.UserType.Teacher, hasPreferredName: true);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Null(doc.GetSummaryListRowForKey("TRN verification level"));
    }
}
