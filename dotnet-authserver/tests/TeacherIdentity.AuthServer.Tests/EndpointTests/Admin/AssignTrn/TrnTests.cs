using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin.AssignTrn;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
public class TrnTests : TestBase
{
    public TrnTests(HostFixture hostFixture)
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
    public async Task Post_TrnNotEntered_ReturnsError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                Trn = trn,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber()
            });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
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

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                Trn = trn,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber()
            });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
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

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                Trn = trn,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber()
            });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Trn", trn }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Trn", "TRN is assigned to another user");
    }

    [Fact]
    public async Task Post_ValidRequest_RedirectsToConfirmPage()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                Trn = trn,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber()
            });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Trn", trn }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/admin/users/{user.UserId}/assign-trn/{trn}", response.Headers.Location?.OriginalString);
    }
}
