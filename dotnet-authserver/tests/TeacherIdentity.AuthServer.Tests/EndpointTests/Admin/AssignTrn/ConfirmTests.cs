using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin.AssignTrn;

public class ConfirmTests : TestBase
{
    public ConfirmTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                Trn = trn,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = Faker.Name.First(),
                MiddleName = "",
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                PendingNameChange = false,
                PendingDateOfBirthChange = false
            });

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn/{trn}");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                Trn = trn,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = Faker.Name.First(),
                MiddleName = "",
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                PendingNameChange = false,
                PendingDateOfBirthChange = false
            });

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn/{trn}");
    }

    [Fact]
    public async Task Get_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var trn = TestData.GenerateTrn();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                Trn = trn,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = Faker.Name.First(),
                MiddleName = "",
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                PendingNameChange = false,
                PendingDateOfBirthChange = false
            });

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{userId}/assign-trn/{trn}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_UserAlreadyHasTrnAssigned_ReturnsBadRequest()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var trn = TestData.GenerateTrn();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                Trn = trn,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = Faker.Name.First(),
                MiddleName = "",
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                PendingNameChange = false,
                PendingDateOfBirthChange = false
            });

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn/{trn}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_TrnDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var trn = TestData.GenerateTrn();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeacherInfo?)null);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{userId}/assign-trn/{trn}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();

        var dqtFirstName = Faker.Name.First();
        var dqtLastName = Faker.Name.Last();
        var dqtDateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var dqtNino = Faker.Identification.UkNationalInsuranceNumber();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = dqtDateOfBirth,
                FirstName = dqtFirstName,
                MiddleName = "",
                LastName = dqtLastName,
                NationalInsuranceNumber = dqtNino,
                Trn = trn,
                PendingNameChange = false,
                PendingDateOfBirthChange = false
            });

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn/{trn}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal($"{user.FirstName} {user.LastName}", doc.GetSummaryListValueForKey("Preferred name"));
        Assert.Equal(user.EmailAddress, doc.GetSummaryListValueForKey("Email address"));
        Assert.Equal($"{dqtFirstName} {dqtLastName}", doc.GetSummaryListValueForKey("DQT name"));
        Assert.Equal(dqtDateOfBirth.ToString("d MMMM yyyy"), doc.GetSummaryListValueForKey("Date of birth"));
        Assert.Equal("Given", doc.GetSummaryListValueForKey("National insurance number"));
        Assert.Equal(trn, doc.GetSummaryListValueForKey("TRN"));
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                Trn = trn,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = Faker.Name.First(),
                MiddleName = "",
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                PendingNameChange = false,
                PendingDateOfBirthChange = false
            });

        await UnauthenticatedUser_RedirectsToSignIn(
            HttpMethod.Post,
            $"/admin/users/{user.UserId}/assign-trn/{trn}",
            new FormUrlEncodedContentBuilder()
            {
                { "AddRecord", bool.TrueString }
            });
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                Trn = trn,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = Faker.Name.First(),
                MiddleName = "",
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                PendingNameChange = false,
                PendingDateOfBirthChange = false
            });

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(
            HttpMethod.Post,
            $"/admin/users/{user.UserId}/assign-trn/{trn}",
            new FormUrlEncodedContentBuilder()
            {
                { "AddRecord", bool.TrueString }
            });
    }

    [Fact]
    public async Task Post_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var trn = TestData.GenerateTrn();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                Trn = trn,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = Faker.Name.First(),
                MiddleName = "",
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                PendingNameChange = false,
                PendingDateOfBirthChange = false
            });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{userId}/assign-trn/{trn}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AddRecord", bool.TrueString }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UserAlreadyHasTrnAssigned_ReturnsBadRequest()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var trn = TestData.GenerateTrn();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                Trn = trn,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = Faker.Name.First(),
                MiddleName = "",
                LastName = Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                PendingNameChange = false,
                PendingDateOfBirthChange = false
            });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn/{trn}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AddRecord", bool.TrueString }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_TrnDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var trn = TestData.GenerateTrn();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeacherInfo?)null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{userId}/assign-trn/{trn}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AddRecord", bool.TrueString }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_NoOptionSelected_ReturnsError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();

        var dqtFirstName = Faker.Name.First();
        var dqtLastName = Faker.Name.Last();
        var dqtDateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var dqtNino = Faker.Identification.UkNationalInsuranceNumber();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = dqtDateOfBirth,
                FirstName = dqtFirstName,
                MiddleName = "",
                LastName = dqtLastName,
                NationalInsuranceNumber = dqtNino,
                Trn = trn,
                PendingNameChange = false,
                PendingDateOfBirthChange = false
            });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn/{trn}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "AddRecord", "Tell us if this is the right DQT record");
    }

    [Fact]
    public async Task Post_WithAddRecordNotConfirmed_RedirectsAndDoesNotAssignTrn()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();

        var dqtFirstName = Faker.Name.First();
        var dqtLastName = Faker.Name.Last();
        var dqtDateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var dqtNino = Faker.Identification.UkNationalInsuranceNumber();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = dqtDateOfBirth,
                FirstName = dqtFirstName,
                MiddleName = "",
                LastName = dqtLastName,
                NationalInsuranceNumber = dqtNino,
                Trn = trn,
                PendingNameChange = false,
                PendingDateOfBirthChange = false
            });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn/{trn}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AddRecord", bool.FalseString }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/admin/users/{user.UserId}", response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            user = await dbContext.Users.SingleAsync(u => u.UserId == user.UserId);
            Assert.Null(user.Trn);
            Assert.Null(user.TrnAssociationSource);
            Assert.NotEqual(TrnLookupStatus.Found, user.TrnLookupStatus);
            Assert.Equal(user.Created, user.Updated);
        });
    }

    [Fact]
    public async Task Post_WithAddRecordConfirmed_AssignsTrnToUserEmitsEventAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();

        var dqtFirstName = Faker.Name.First();
        var dqtLastName = Faker.Name.Last();
        var dqtDateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var dqtNino = Faker.Identification.UkNationalInsuranceNumber();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = dqtDateOfBirth,
                FirstName = dqtFirstName,
                MiddleName = "",
                LastName = dqtLastName,
                NationalInsuranceNumber = dqtNino,
                Trn = trn,
                PendingNameChange = false,
                PendingDateOfBirthChange = false
            });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn/{trn}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AddRecord", bool.TrueString }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/admin/users/{user.UserId}", response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            user = await dbContext.Users.SingleAsync(u => u.UserId == user.UserId);
            Assert.Equal(trn, user.Trn);
            Assert.Equal(TrnAssociationSource.SupportUi, user.TrnAssociationSource);
            Assert.Equal(TrnLookupStatus.Found, user.TrnLookupStatus);
            Assert.Equal(Clock.UtcNow, user.Updated);
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                Assert.Equal(UserUpdatedEventSource.SupportUi, userUpdatedEvent.Source);
                Assert.Equal(UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus, userUpdatedEvent.Changes);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            });
    }
}
