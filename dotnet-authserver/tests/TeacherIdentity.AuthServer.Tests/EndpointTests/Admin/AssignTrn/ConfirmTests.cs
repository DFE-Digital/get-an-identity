using System.Diagnostics;
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
        ConfigureDqtApiMock(trn);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}");
    }

    [Fact]
    public async Task Get_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{userId}/assign-trn/confirm?trn={trn}");

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
        ConfigureDqtApiMock(trn);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}");

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

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{userId}/assign-trn/confirm?trn={trn}");

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
        var dqtMiddleName = Faker.Name.Middle();
        var dqtLastName = Faker.Name.Last();
        var dqtDateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var dqtNino = Faker.Identification.UkNationalInsuranceNumber();
        var dqtEmail = Faker.Internet.Email();

        ConfigureDqtApiMock(trn, dqtDateOfBirth, dqtFirstName, dqtMiddleName, dqtLastName, dqtNino, dqtEmail);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        var idSection = doc.GetElementByTestId("IdSection");
        Assert.Equal(user.EmailAddress, idSection?.GetSummaryListValueForKey("Email address"));
        Assert.Equal($"{user.FirstName} {user.MiddleName} {user.LastName}", idSection?.GetSummaryListValueForKey("Name"));
        Assert.Equal(user.DateOfBirth!.Value.ToString("d MMMM yyyy"), idSection?.GetSummaryListValueForKey("Date of birth"));
        var dqtSection = doc.GetElementByTestId("DqtSection");
        Assert.Equal(trn, dqtSection?.GetSummaryListValueForKey("TRN"));
        Assert.Equal(dqtEmail, dqtSection?.GetSummaryListValueForKey("Email address"));
        Assert.Equal($"{dqtFirstName} {dqtMiddleName} {dqtLastName}", dqtSection?.GetSummaryListValueForKey("Name"));
        Assert.Equal(dqtDateOfBirth.ToString("d MMMM yyyy"), dqtSection?.GetSummaryListValueForKey("Date of birth"));
    }

    [Fact]
    public async Task Get_DqtEmailDifferentToIdEmail_ShowsWarning()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();

        var dqtEmail = Faker.Internet.Email();
        Debug.Assert(dqtEmail != user.EmailAddress);

        ConfigureDqtApiMock(trn, email: dqtEmail);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.NotNull(doc.GetElementByTestId("EmailOverwriteWarning"));
    }

    [Fact]
    public async Task Get_DqtEmailSameAsIdEmail_DoesNotShowWarning()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn, email: user.EmailAddress);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Null(doc.GetElementByTestId("EmailOverwriteWarning"));
    }

    [Fact]
    public async Task Get_DqtEmailEmpty_DoesNotShowWarning()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn, email: "");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Null(doc.GetElementByTestId("EmailOverwriteWarning"));
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        await UnauthenticatedUser_RedirectsToSignIn(
            HttpMethod.Post,
            $"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}",
            new FormUrlEncodedContentBuilder()
            {
                { "AssignTrn", bool.TrueString }
            });
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(
            HttpMethod.Post,
            $"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}",
            new FormUrlEncodedContentBuilder()
            {
                { "AssignTrn", bool.TrueString }
            });
    }

    [Fact]
    public async Task Post_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{userId}/assign-trn/confirm?trn={trn}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AssignTrn", bool.TrueString }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_WithTrnAndUserAlreadyHasTrnAssigned_ReturnsBadRequest()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AssignTrn", bool.TrueString }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_WithoutTrnAndUserAlreadyHasTrnAssigned_ReturnsBadRequest()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn/confirm")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "ConfirmNoTrn", bool.TrueString }
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

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{userId}/assign-trn/confirm?trn={trn}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AssignTrn", bool.TrueString }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_WithTrnAndNoOptionSelected_ReturnsError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "AssignTrn", "Tell us if you want to assign this TRN");
    }

    [Fact]
    public async Task Post_WithTrnAndAddRecordNotConfirmed_RedirectsAndDoesNotAssignTrn()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AssignTrn", bool.FalseString }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/admin/users/{user.UserId}/assign-trn?hasTrn=True", response.Headers.Location?.OriginalString);

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
    public async Task Post_WithTrnAndAddRecordConfirmedAndDifferentDqtName_UpdatesUserNameAndTrnAndEmitsEventAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();

        var dqtFirstName = Faker.Name.First();
        var dqtMiddleName = Faker.Name.Middle();
        var dqtLastName = Faker.Name.Last();
        var dqtDateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var dqtNino = Faker.Identification.UkNationalInsuranceNumber();
        var dqtEmail = Faker.Internet.Email();

        ConfigureDqtApiMock(trn, dqtDateOfBirth, dqtFirstName, dqtMiddleName, dqtLastName, dqtNino, dqtEmail);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AssignTrn", bool.TrueString }
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
            Assert.Equal(dqtFirstName, user.FirstName);
            Assert.Equal(dqtMiddleName, user.MiddleName);
            Assert.Equal(dqtLastName, user.LastName);
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
                Assert.Equal(UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus | UserUpdatedEventChanges.FirstName | UserUpdatedEventChanges.MiddleName | UserUpdatedEventChanges.LastName, userUpdatedEvent.Changes);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            });
    }

    [Fact]
    public async Task Post_WithTrnAndAddRecordConfirmedAndDqtNameMatch_AssignsTrnToUserAndEmitsEventAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();
        var dqtFirstName = user.FirstName;
        var dqtMiddleName = user.MiddleName;
        var dqtLastName = user.LastName;
        var dqtDateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var dqtNino = Faker.Identification.UkNationalInsuranceNumber();
        var dqtEmail = Faker.Internet.Email();

        ConfigureDqtApiMock(trn, dqtDateOfBirth, dqtFirstName, dqtMiddleName, dqtLastName, dqtNino, dqtEmail);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn/confirm?trn={trn}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AssignTrn", bool.TrueString }
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

    [Fact]
    public async Task Post_WithoutTrnAndNotConfirmed_ReturnsError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn/confirm")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "ConfirmNoTrn", "Confirm the user does not have a TRN");
    }

    [Fact]
    public async Task Post_WithoutTrnAndConfirmed_UpdatesTrnLookupStatusEmitsEventAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var trn = TestData.GenerateTrn();
        ConfigureDqtApiMock(trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/assign-trn/confirm")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "ConfirmNoTrn", bool.TrueString }
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
            Assert.Equal(TrnAssociationSource.SupportUi, user.TrnAssociationSource);
            Assert.Equal(TrnLookupStatus.Failed, user.TrnLookupStatus);
            Assert.Equal(Clock.UtcNow, user.Updated);
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                Assert.Equal(UserUpdatedEventSource.SupportUi, userUpdatedEvent.Source);
                Assert.Equal(UserUpdatedEventChanges.TrnLookupStatus, userUpdatedEvent.Changes);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            });
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
                Email = email ?? Faker.Internet.Email()
            });
    }
}
