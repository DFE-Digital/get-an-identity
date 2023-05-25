using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api.V1.Users;

public class UpdateUserTests : TestBase
{
    public UpdateUserTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Theory]
    [MemberData(nameof(NotPermittedScopes))]
    public async Task Patch_UserDoesNotHavePermission_ReturnsForbidden(string scope)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(withUser: true, scope);

        var user = await TestData.CreateUser(userType: Models.UserType.Default);
        var updatedEmail = Faker.Internet.Email();
        var updatedFirstName = Faker.Name.First();
        var updatedLastName = Faker.Name.Last();

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/{user.UserId}")
        {
            Content = JsonContent.Create(new
            {
                email = updatedEmail,
                firstName = updatedFirstName,
                lastName = updatedLastName
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    [Fact]
    public async Task Patch_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(withUser: true, PermittedScopes.First());

        var userId = Guid.NewGuid();
        var updatedEmail = Faker.Internet.Email();
        var updatedFirstName = Faker.Name.First();
        var updatedLastName = Faker.Name.Last();

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/{userId}")
        {
            Content = JsonContent.Create(new
            {
                email = updatedEmail,
                firstName = updatedFirstName,
                lastName = updatedLastName
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        await AssertEx.JsonResponseIsError(response, 10003, StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Patch_UserIsNotDefaultUserType_ReturnsForbidden()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(withUser: true, PermittedScopes.First());

        var user = await TestData.CreateUser(userType: Models.UserType.Staff);
        var updatedEmail = Faker.Internet.Email();
        var updatedFirstName = Faker.Name.First();
        var updatedLastName = Faker.Name.Last();

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/{user.UserId}")
        {
            Content = JsonContent.Create(new
            {
                email = updatedEmail,
                firstName = updatedFirstName,
                lastName = updatedLastName
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        await AssertEx.JsonResponseIsError(response, 10005, StatusCodes.Status403Forbidden);
    }

    [Theory]
    [MemberData(nameof(GetInvalidRequestData))]
    public async Task Patch_RequestIsInvalid_ReturnsError(
        string email,
        string firstName,
        string lastName,
        string expectedErrorField,
        string expectedErrorMessage)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(withUser: true, PermittedScopes.First());

        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/{user.UserId}")
        {
            Content = JsonContent.Create(new
            {
                email = email,
                firstName = firstName,
                lastName = lastName
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        await AssertEx.JsonResponseHasValidationErrorForProperty(response, expectedErrorField, expectedErrorMessage);
    }

    [Theory]
    [MemberData(nameof(PermittedScopes))]
    public async Task Patch_ValidRequestWithAllFields_UpdatesUserAndReturnsUpdatedDetails(string scope)
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(withUser: true, scope);

        var user = await TestData.CreateUser(userType: Models.UserType.Default);
        var updatedEmail = Faker.Internet.Email();
        var updatedFirstName = Faker.Name.First();
        var updatedMiddleName = Faker.Name.Middle();
        var updatedLastName = Faker.Name.Last();

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/{user.UserId}")
        {
            Content = JsonContent.Create(new
            {
                email = updatedEmail,
                firstName = updatedFirstName,
                middleName = updatedMiddleName,
                lastName = updatedLastName
            })
        };

        // Advance the clock to ensure the update time is different to the user's created time
        Clock.AdvanceBy(TimeSpan.FromMinutes(1));

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        var responseObj = await AssertEx.JsonResponse(response);
        Assert.Equal(user.UserId, responseObj.RootElement.GetProperty("userId").GetGuid());
        Assert.Equal(updatedEmail, responseObj.RootElement.GetProperty("email").GetString());
        Assert.Equal(updatedFirstName, responseObj.RootElement.GetProperty("firstName").GetString());
        Assert.Equal(updatedMiddleName, responseObj.RootElement.GetProperty("middleName").GetString());
        Assert.Equal(updatedLastName, responseObj.RootElement.GetProperty("lastName").GetString());

        user = await TestData.WithDbContext(dbContext => dbContext.Users.SingleAsync(u => u.UserId == user.UserId));
        Assert.Equal(Clock.UtcNow, user.Updated);

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                Assert.Equal(UserUpdatedEventSource.Api, userUpdatedEvent.Source);
                Assert.Equal(UserUpdatedEventChanges.EmailAddress | UserUpdatedEventChanges.FirstName | UserUpdatedEventChanges.LastName | UserUpdatedEventChanges.MiddleName, userUpdatedEvent.Changes);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
                Assert.Equal(TestClients.DefaultClient.ClientId, userUpdatedEvent.UpdatedByClientId);
                Assert.Equal(TestUsers.AdminUserWithAllRoles.UserId, userUpdatedEvent.UpdatedByUserId);
            });
    }

    [Fact]
    public async Task Patch_WithNoFieldsSpecified_ReturnsUserDetails()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(withUser: true, PermittedScopes.First());

        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/{user.UserId}")
        {
            Content = JsonContent.Create(new { })
        };

        // Advance the clock to ensure the update time is different to the user's created time
        Clock.AdvanceBy(TimeSpan.FromMinutes(1));

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        var responseObj = await AssertEx.JsonResponse(response);
        Assert.Equal(user.UserId, responseObj.RootElement.GetProperty("userId").GetGuid());
        Assert.Equal(user.EmailAddress, responseObj.RootElement.GetProperty("email").GetString());
        Assert.Equal(user.FirstName, responseObj.RootElement.GetProperty("firstName").GetString());
        Assert.Equal(user.LastName, responseObj.RootElement.GetProperty("lastName").GetString());

        user = await TestData.WithDbContext(dbContext => dbContext.Users.SingleAsync(u => u.UserId == user.UserId));
        Assert.Equal(user.Created, user.Updated);
    }

    public static TheoryData<string> NotPermittedScopes => ScopeTheoryData.GetAllStaffUserScopesExcept(PermittedScopes);

    public static TheoryData<string> PermittedScopes => ScopeTheoryData.Single(CustomScopes.UserWrite);

    public static IEnumerable<object[]> GetInvalidRequestData()
    {
        var validEmail = Faker.Internet.Email();
        var validFirstName = Faker.Name.First();
        var validLastName = Faker.Name.Last();

        // Invalid email
        yield return new InvalidRequestDataWrapper()
        {
            Email = "xxx",
            FirstName = validFirstName,
            LastName = validLastName,
            ExpectedErrorField = "email",
            ExpectedErrorMessage = "Email is not valid."
        };

        // Email too long
        yield return new InvalidRequestDataWrapper()
        {
            Email = "joe@bloggs" + new string('x', 201 - "joe@bloggs".Length),
            FirstName = validFirstName,
            LastName = validLastName,
            ExpectedErrorField = "email",
            ExpectedErrorMessage = "Email must be 200 characters or less."
        };

        // First name too long
        yield return new InvalidRequestDataWrapper()
        {
            Email = validEmail,
            FirstName = new string('x', 201),
            LastName = validLastName,
            ExpectedErrorField = "firstName",
            ExpectedErrorMessage = "First name must be 200 characters or less."
        };

        // Last name too long
        yield return new InvalidRequestDataWrapper()
        {
            Email = validEmail,
            FirstName = validFirstName,
            LastName = new string('x', 201),
            ExpectedErrorField = "lastName",
            ExpectedErrorMessage = "Last name must be 200 characters or less."
        };
    }

    public class InvalidRequestDataWrapper
    {
        public string? Email { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public required string ExpectedErrorField { get; init; }
        public required string ExpectedErrorMessage { get; init; }

        public static implicit operator object?[](InvalidRequestDataWrapper data) =>
            new object?[] { data.Email, data.FirstName, data.LastName, data.ExpectedErrorField, data.ExpectedErrorMessage };
    }
}
