using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin.AssignTrn;

public class IndexTests : TestBase
{
    public IndexTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn");
    }

    [Fact]
    public async Task Get_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{userId}/assign-trn");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_UserAlreadyHasTrnAssigned_ReturnsBadRequest()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: true);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsOk()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn");
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn");
    }

    [Fact]
    public async Task Post_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{userId}/assign-trn");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UserAlreadyHasTrnAssigned_ReturnsBadRequest()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: true);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_HasTrnNotAnswered_ReturnsError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "HasTrn", "Tell us if the user has a TRN");
    }

    [Fact]
    public async Task Post_HasTrnButNoTrnNoEntered_ReturnsError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasTrn", bool.TrueString }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Trn", "Enter a TRN");
    }

    [Theory]
    [InlineData("xxx")]
    [InlineData("1")]
    [InlineData("12345678")]
    public async Task Post_TrnNotValid_ReturnsError(string trn)
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasTrn", bool.TrueString },
                { "Trn", trn }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Trn", "TRN must be 7 digits");
    }

    [Fact]
    public async Task Post_TrnDoesNotExist_ReturnsError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeacherInfo?)null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasTrn", bool.TrueString },
                { "Trn", trn }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Trn", "TRN does not exist");
    }

    [Fact]
    public async Task Post_TrnAlreadyAllocated_ReturnsError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);
        var anotherUser = await TestData.CreateUser(hasTrn: true);
        var trn = anotherUser.Trn!;
        ConfigureDqtApiMock(trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasTrn", bool.TrueString },
                { "Trn", trn }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Trn", "TRN is assigned to another user");
    }

    [Fact]
    public async Task Post_ValidRequestWithTrn_RedirectsToConfirmPage()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasTrn", bool.TrueString },
                { "Trn", trn }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_ValidRequestWithNoTrn_RedirectsToConfirmPage()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasTrn", bool.FalseString }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/admin/users/{user.UserId}/assign-trn/confirm", response.Headers.Location?.OriginalString);
    }

    private void ConfigureDqtApiMock(
        string trn,
        DateOnly? dateOfBirth = null,
        string? firstName = null,
        string? middleName = null,
        string? lastName = null,
        string? nino = null,
        string? email = null)
    {
        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = dateOfBirth ?? DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = firstName ?? Faker.Name.First(),
                MiddleName = middleName ?? Faker.Name.Middle(),
                LastName = lastName ?? Faker.Name.Last(),
                NationalInsuranceNumber = nino ?? Faker.Identification.UkNationalInsuranceNumber(),
                Trn = trn,
                PendingNameChange = false,
                PendingDateOfBirthChange = false,
                Email = email ?? Faker.Internet.Email(),
                Alerts = Array.Empty<AlertInfo>(),
                AllowIdSignInWithProhibitions = false
            });
    }
}
