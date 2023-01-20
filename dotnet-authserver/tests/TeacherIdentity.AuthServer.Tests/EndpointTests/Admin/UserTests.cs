namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
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
        var user = await TestData.CreateUser(hasTrn: false, userType: Models.UserType.Default);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal($"{user.FirstName} {user.LastName}", doc.GetElementByTestId("Name")?.TextContent);
        Assert.Equal(user.EmailAddress, doc.GetSummaryListValueForKey("Email address"));
        Assert.Equal("No", doc.GetSummaryListValueForKey("DQT record"));
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
                LastName = dqtLastName,
                NationalInsuranceNumber = dqtNino,
                Trn = user.Trn!
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
        Assert.Equal(dqtDateOfBirth.ToString("d MMMM yyyy"), doc.GetSummaryListValueForKey("Date of birth"));
        Assert.Equal("Given", doc.GetSummaryListValueForKey("National insurance number"));
        Assert.Equal(user.Trn, doc.GetSummaryListValueForKey("TRN"));
    }
}
