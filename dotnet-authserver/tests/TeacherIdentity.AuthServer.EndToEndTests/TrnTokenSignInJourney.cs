using Microsoft.Playwright;
using Moq;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class TrnTokenSignInJourney : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public TrnTokenSignInJourney(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
        _hostFixture.OnTestStarting();
    }

    [Fact]
    public async Task NewTeacherUser_WithTrnToken_CreatesUserAndCompletesFlow()
    {
        var mobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();
        var firstName = Faker.Name.First();
        var middleName = Faker.Name.Middle();
        var lastName = Faker.Name.Last();
        var preferredName = Faker.Name.FullName();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = _hostFixture.TestData.GenerateTrn();

        var trnToken = await _hostFixture.TestData.GenerateTrnToken(trn);

        _hostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = dateOfBirth,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Trn = trn,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                PendingDateOfBirthChange = false,
                PendingNameChange = false,
                Email = trnToken.Email
            });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.RegisterFromTrnTokenLandingPage();

        await page.SubmitRegisterPhonePage(mobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitRegisterPreferredNamePage();

        await page.SubmitTrnTokenCheckAnswersPage();

        await page.SubmitCompletePageForNewUser(trnToken.Email);

        await page.AssertSignedInOnTestClient(trnToken.Email, trn, firstName, lastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.NotNull(trnTokenModel?.UserId);
    }

    [Fact]
    public async Task NewTeacherUser_WithTrnTokenForDqtRecordWithMissingDateOfBirth_CreatesUserAndCompletesFlow()
    {
        var mobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();
        var firstName = Faker.Name.First();
        var middleName = Faker.Name.Middle();
        var lastName = Faker.Name.Last();
        var preferredName = Faker.Name.FullName();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = _hostFixture.TestData.GenerateTrn();

        var trnToken = await _hostFixture.TestData.GenerateTrnToken(trn);

        _hostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = null,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Trn = trn,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                PendingDateOfBirthChange = false,
                PendingNameChange = false,
                Email = trnToken.Email
            });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.RegisterFromTrnTokenLandingPage();

        await page.SubmitRegisterPhonePage(mobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitRegisterPreferredNamePage();

        await page.SubmitDateOfBirthPage(dateOfBirth);

        await page.SubmitTrnTokenCheckAnswersPage();

        await page.SubmitCompletePageForNewUser(trnToken.Email);

        await page.AssertSignedInOnTestClient(trnToken.Email, trn, firstName, lastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.NotNull(trnTokenModel?.UserId);
    }

    [Fact]
    public async Task NewTeacherUser_WithTrnTokenChangesEmail_CreatesUserAndCompletesFlow()
    {
        var mobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();
        var firstName = Faker.Name.First();
        var middleName = Faker.Name.Middle();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = _hostFixture.TestData.GenerateTrn();

        var trnToken = await _hostFixture.TestData.GenerateTrnToken(trn);

        _hostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = dateOfBirth,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Trn = trn,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                PendingDateOfBirthChange = false,
                PendingNameChange = false,
                Email = trnToken.Email
            });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.RegisterFromTrnTokenLandingPage();

        await page.SubmitRegisterPhonePage(mobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitRegisterPreferredNamePage();

        await page.ClickChangeLinkTrnTokenCheckAnswersPage("trn-token-email-change-link");

        var newEmail = Faker.Internet.Email();

        await page.SubmitRegisterEmailPage(newEmail);

        await page.SubmitRegisterEmailConfirmationPage();

        await page.SubmitTrnTokenCheckAnswersPage();

        await page.SubmitCompletePageForNewUser(newEmail);

        await page.AssertSignedInOnTestClient(newEmail, trn, firstName, lastName);
    }

    [Fact]
    public async Task SignedInUserWithTrn_ValidTrnTokenMatchingEmailAndTrn_InvalidatesToken()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        await SignInUser(user, page, context);

        var trnToken = await CreateValidTrnToken(trn: user.Trn, email: user.EmailAddress, firstName: user.FirstName, middleName: user.MiddleName, lastName: user.LastName);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user, expectTrn: true);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Equal(user.UserId, trnTokenModel?.UserId);
    }

    [Fact]
    public async Task SignedInUserNoTrn_ValidTrnTokenMatchingEmail_UpdatesUserTrnInvalidatesToken()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        await SignInUser(user, page, context);

        var trnToken = await CreateValidTrnToken(email: user.EmailAddress, firstName: user.FirstName, middleName: user.MiddleName, lastName: user.LastName);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user.EmailAddress, trnToken.Trn, user.FirstName, user.LastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Equal(user.UserId, trnTokenModel?.UserId);

        _hostFixture.EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus, userUpdatedEvent.Changes);
                Assert.Equal(trnTokenModel?.Trn, userUpdatedEvent.User.Trn);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            },
            e => Assert.IsType<UserSignedInEvent>(e));
    }

    [Fact]
    public async Task SignedInUserWithTrn_ValidTrnTokenMatchingEmailNotMatchingTrn_IgnoresTrnToken()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var differentTrn = _hostFixture.TestData.GenerateTrn();

        await SignInUser(user, page, context);

        var trnToken = await CreateValidTrnToken(trn: differentTrn, email: user.EmailAddress);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user, expectTrn: true);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Null(trnTokenModel?.UserId);
    }

    [Fact]
    public async Task SignedInUser_ValidTrnTokenNotMatchingEmail_StartsTrnTokenJourney()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        var differentEmail = _hostFixture.TestData.GenerateUniqueEmail();
        var differentMobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();
        var differentFirstName = Faker.Name.First();
        var differentLastName = Faker.Name.Last();

        await SignInUser(user, page, context);

        var trnToken = await CreateValidTrnToken(email: differentEmail, firstName: differentFirstName, lastName: differentLastName);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.RegisterFromTrnTokenLandingPage();

        await page.SubmitRegisterPhonePage(differentMobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitRegisterPreferredNamePage();

        await page.SubmitTrnTokenCheckAnswersPage();

        await page.SubmitCompletePageForNewUser(differentEmail);

        await page.AssertSignedInOnTestClient(differentEmail, trnToken.Trn, differentFirstName, differentLastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.NotNull(trnTokenModel?.UserId);
    }

    [Fact]
    public async Task ExistingUserWithTrn_ValidTrnTokenMatchingEmailAndTrn_SignsInUserInvalidatesToken()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        var trnToken = await CreateValidTrnToken(trn: user.Trn, email: user.EmailAddress, firstName: user.FirstName, middleName: user.MiddleName, lastName: user.LastName);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user.EmailAddress, user.Trn, user.FirstName, user.LastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Equal(user.UserId, trnTokenModel?.UserId);

        _hostFixture.EventObserver.AssertEventsSaved(e => Assert.IsType<UserSignedInEvent>(e));
    }

    [Fact]
    public async Task ExistingUserNoTrn_ValidTrnTokenMatchingEmail_SignsInUserUpdatesTrnInvalidatesToken()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        var trnToken = await CreateValidTrnToken(email: user.EmailAddress, firstName: user.FirstName, middleName: user.MiddleName, lastName: user.LastName);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user.EmailAddress, trnToken.Trn, user.FirstName, user.LastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Equal(user.UserId, trnTokenModel?.UserId);

        _hostFixture.EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus, userUpdatedEvent.Changes);
                Assert.Equal(trnTokenModel?.Trn, userUpdatedEvent.User.Trn);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            },
            e => Assert.IsType<UserSignedInEvent>(e));
    }

    [Fact]
    public async Task ExistingUserNoTrn_ValidTrnTokenMatchingEmailNotMatchingName_SignsInUserUpdatesTrnAndNameInvalidatesToken()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        var dqtFirstName = Faker.Name.First();
        var dqtLastName = Faker.Name.Last();

        var trnToken = await CreateValidTrnToken(email: user.EmailAddress, firstName: dqtFirstName, lastName: dqtLastName);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user.EmailAddress, trnToken.Trn, dqtFirstName, dqtLastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Equal(user.UserId, trnTokenModel?.UserId);

        _hostFixture.EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus | UserUpdatedEventChanges.FirstName | UserUpdatedEventChanges.MiddleName | UserUpdatedEventChanges.LastName, userUpdatedEvent.Changes);
                Assert.Equal(trnTokenModel?.Trn, userUpdatedEvent.User.Trn);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            },
            e => Assert.IsType<UserSignedInEvent>(e));
    }

    [Fact]
    public async Task ExistingUserWithTrn_ValidTrnTokenNotMatchingTrn_StartsCoreSignInJourney()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var differentTrn = _hostFixture.TestData.GenerateTrn();

        var trnToken = await CreateValidTrnToken(trn: differentTrn, email: user.EmailAddress);

        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var trn = _hostFixture.TestData.GenerateTrn();

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await CompleteCoreSignInJourneyWithTrnLookup(page, email, firstName, lastName, trn, trnToken);

        await page.SubmitCompletePageForNewUser(email);

        await page.AssertSignedInOnTestClient(email, trn, firstName, lastName);

        _hostFixture.EventObserver.AssertEventsSaved(
            e =>
            {
                var userRegisteredEvent = Assert.IsType<UserRegisteredEvent>(e);
                Assert.Equal(_hostFixture.TestClientId, userRegisteredEvent.ClientId);
                Assert.Equal(email, userRegisteredEvent.User.EmailAddress);
                Assert.Equal(firstName, userRegisteredEvent.User.FirstName);
                Assert.Equal(lastName, userRegisteredEvent.User.LastName);
                Assert.Equal(trn, userRegisteredEvent.User.Trn);
            },
            e => Assert.IsType<UserSignedInEvent>(e));

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Null(trnTokenModel?.UserId);
    }

    [Fact]
    public async Task ExistingUserSignsInNoTrn_ValidTrnToken_AssignsToken()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);
        var differentEmail = _hostFixture.TestData.GenerateUniqueEmail();

        var trnToken = await CreateValidTrnToken(email: differentEmail, firstName: user.FirstName, middleName: user.MiddleName, lastName: user.LastName);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.SignInFromTrnTokenLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user.EmailAddress, trnToken.Trn, user.FirstName, user.LastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Equal(user.UserId, trnTokenModel?.UserId);

        _hostFixture.EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus, userUpdatedEvent.Changes);
                Assert.Equal(trnTokenModel?.Trn, userUpdatedEvent.User.Trn);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            },
            e => Assert.IsType<UserSignedInEvent>(e));
    }

    [Fact]
    public async Task ExistingUserSignsInNoTrn_ValidTrnTokenDifferentName_AssignsTokenUpdatesName()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);
        var differentEmail = _hostFixture.TestData.GenerateUniqueEmail();

        var dqtFirstName = Faker.Name.First();
        var dqtLastName = Faker.Name.Last();

        var trnToken = await CreateValidTrnToken(email: differentEmail, firstName: dqtFirstName, lastName: dqtLastName);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.SignInFromTrnTokenLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user.EmailAddress, trnToken.Trn, dqtFirstName, dqtLastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Equal(user.UserId, trnTokenModel?.UserId);

        _hostFixture.EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus | UserUpdatedEventChanges.FirstName | UserUpdatedEventChanges.MiddleName | UserUpdatedEventChanges.LastName, userUpdatedEvent.Changes);
                Assert.Equal(trnTokenModel?.Trn, userUpdatedEvent.User.Trn);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            },
            e => Assert.IsType<UserSignedInEvent>(e));
    }

    [Fact]
    public async Task ExistingUserSignsIn_ValidTrnTokenNotMatchingTrn_SignInAndIgnoreToken()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        var differentTrn = _hostFixture.TestData.GenerateTrn();
        var differentEmail = _hostFixture.TestData.GenerateUniqueEmail();

        var trnToken = await CreateValidTrnToken(trn: differentTrn, email: differentEmail);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.SignInFromTrnTokenLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user.EmailAddress, user.Trn, user.FirstName, user.LastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Null(trnTokenModel?.UserId);
    }

    [Fact]
    public async Task ExistingUserSignsIn_ValidTrnTokenMatchingTrn_SignInAndInvalidatesToken()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        var differentEmail = _hostFixture.TestData.GenerateUniqueEmail();

        var trnToken = await CreateValidTrnToken(trn: user.Trn, email: differentEmail, firstName: user.FirstName, middleName: user.MiddleName, lastName: user.LastName);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.SignInFromTrnTokenLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user.EmailAddress, user.Trn, user.FirstName, user.LastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Equal(user.UserId, trnTokenModel?.UserId);
    }

    [Fact]
    public async Task NewUserSignsIn_ValidTrnTokenMatchingExistingAccountNameAndDoBNoTrn_SignInAndAssignTrnToken()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        var trnToken = await CreateValidTrnToken(dateOfBirth: user.DateOfBirth, firstName: user.FirstName, middleName: user.MiddleName, lastName: user.LastName);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.RegisterFromTrnTokenLandingPage();

        await page.SubmitAccountExistsPage(isUsersAccount: true);

        await page.SubmitExistingAccountEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user.EmailAddress, trnToken.Trn, user.FirstName, user.LastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Equal(user.UserId, trnTokenModel?.UserId);

        _hostFixture.EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus, userUpdatedEvent.Changes);
                Assert.Equal(trnTokenModel?.Trn, userUpdatedEvent.User.Trn);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            },
            e => Assert.IsType<UserSignedInEvent>(e));
    }

    [Fact]
    public async Task NewUserSignsIn_ValidTrnTokenMatchingExistingAccountNameAndDoBWithNotMatchingTrn_SignInAndIgnoreTrnToken()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        var differentTrn = _hostFixture.TestData.GenerateTrn();

        var trnToken = await CreateValidTrnToken(trn: differentTrn, firstName: user.FirstName, lastName: user.LastName, dateOfBirth: user.DateOfBirth);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.RegisterFromTrnTokenLandingPage();

        await page.SubmitAccountExistsPage(isUsersAccount: true);

        await page.SubmitExistingAccountEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Null(trnTokenModel?.UserId);
    }

    [Fact]
    public async Task NewUserSignsIn_ValidTrnTokenMatchingExistingAccountNameAndDoBWithNotMatchingTrn_ExistingAccountNotChosen_ContinuesTrnTokenSignInJourney()
    {
        var mobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        var differentTrn = _hostFixture.TestData.GenerateTrn();

        var trnToken = await CreateValidTrnToken(trn: differentTrn, firstName: user.FirstName, lastName: user.LastName, dateOfBirth: user.DateOfBirth);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.RegisterFromTrnTokenLandingPage();

        await page.SubmitAccountExistsPage(isUsersAccount: false);

        await page.SubmitRegisterPhonePage(mobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitRegisterPreferredNamePage();

        await page.SubmitTrnTokenCheckAnswersPage();

        await page.SubmitCompletePageForNewUser(trnToken.Email);

        await page.AssertSignedInOnTestClient(trnToken.Email, trnToken.Trn, user.FirstName, user.LastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.NotNull(trnTokenModel?.UserId);
    }

    [Fact]
    public async Task NewUserRegisters_ValidTrnTokenPhoneVerificationMatchesExistingAccountNoTrn_AssignsTrnToken()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        var trnToken = await CreateValidTrnToken(firstName: user.FirstName, middleName: user.MiddleName, lastName: user.LastName);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.RegisterFromTrnTokenLandingPage();

        await page.SubmitRegisterPhonePage(user.MobileNumber!);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SignInFromRegisterPhoneExistsPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user.EmailAddress, trnToken.Trn, user.FirstName, user.LastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Equal(user.UserId, trnTokenModel?.UserId);

        _hostFixture.EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus, userUpdatedEvent.Changes);
                Assert.Equal(trnTokenModel?.Trn, userUpdatedEvent.User.Trn);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            },
            e => Assert.IsType<UserSignedInEvent>(e));
    }

    [Fact]
    public async Task NewUserRegisters_ValidTrnTokenPhoneVerificationMatchesExistingAccountMatchingTrn_InvalidatesTrnToken()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        var trnToken = await CreateValidTrnToken(trn: user.Trn, firstName: user.FirstName, middleName: user.MiddleName, lastName: user.LastName);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.RegisterFromTrnTokenLandingPage();

        await page.SubmitRegisterPhonePage(user.MobileNumber!);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SignInFromRegisterPhoneExistsPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user.EmailAddress, trnToken.Trn, user.FirstName, user.LastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Equal(user.UserId, trnTokenModel?.UserId);

        _hostFixture.EventObserver.AssertEventsSaved(e => Assert.IsType<UserSignedInEvent>(e));
    }

    [Fact]
    public async Task NewUserRegisters_ValidTrnTokenPhoneVerificationMatchesExistingAccountNotMatchingTrn_IgnoresTrnToken()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        var differentTrn = _hostFixture.TestData.GenerateTrn();
        var trnToken = await CreateValidTrnToken(differentTrn);

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.RegisterFromTrnTokenLandingPage();

        await page.SubmitRegisterPhonePage(user.MobileNumber!);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SignInFromRegisterPhoneExistsPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user.EmailAddress, user.Trn, user.FirstName, user.LastName);

        var trnTokenModel = await _hostFixture.TestData.GetTrnToken(trnToken.TrnToken);
        Assert.Null(trnTokenModel?.UserId);

        _hostFixture.EventObserver.AssertEventsSaved(e => Assert.IsType<UserSignedInEvent>(e));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NewTeacherUser_WithTrnTokenChangesToInstitutionEmail_CreatesUserAndCompletesFlow(bool useInstitutionEmail)
    {
        var mobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();
        var firstName = Faker.Name.First();
        var middleName = Faker.Name.Middle();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = _hostFixture.TestData.GenerateTrn();

        var trnToken = await _hostFixture.TestData.GenerateTrnToken(trn);

        _hostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = dateOfBirth,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Trn = trn,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                PendingDateOfBirthChange = false,
                PendingNameChange = false,
                Email = trnToken.Email
            });

        var invalidEmailSuffix = "invalid.sch.uk";
        await _hostFixture.TestData.EnsureEstablishmentDomain(invalidEmailSuffix);

        var newInstitutionEmail = $"{Faker.Internet.Email().Split('@')[0]}@{invalidEmailSuffix}";
        var newPersonalEmail = Faker.Internet.Email();
        var newExpectedEmail = useInstitutionEmail ? newInstitutionEmail : newPersonalEmail;

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.RegisterFromTrnTokenLandingPage();

        await page.SubmitRegisterPhonePage(mobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitRegisterPreferredNamePage();

        await page.ClickChangeLinkTrnTokenCheckAnswersPage("trn-token-email-change-link");

        await page.SubmitRegisterEmailPage(newInstitutionEmail);

        await page.SubmitRegisterEmailConfirmationPage();

        if (useInstitutionEmail)
        {
            await page.SubmitRegisterInstitutionEmailPage(useInstitutionEmail);
        }
        else
        {
            await page.SubmitRegisterInstitutionEmailPage(useInstitutionEmail, newPersonalEmail);

            await page.SubmitRegisterEmailConfirmationPage();
        }

        await page.SubmitTrnTokenCheckAnswersPage();

        await page.SubmitCompletePageForNewUser(newExpectedEmail);

        await page.AssertSignedInOnTestClient(newExpectedEmail, trn, firstName, lastName);
    }

    private async Task SignInUser(User user, IPage page, IBrowserContext context)
    {
        await page.StartOAuthJourney(additionalScope: null);

        await page.SignInFromLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user, expectTrn: false);

        await ClearCookiesForTestClient(context);

        _hostFixture.EventObserver.Clear();
    }

    private async Task CompleteCoreSignInJourneyWithTrnLookup(
        IPage page,
        string email,
        string firstName,
        string lastName,
        string trn,
        TrnTokenModel trnToken)
    {
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var mobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();

        ConfigureDqtApiFindTeachersRequest(result: new()
        {
            DateOfBirth = dateOfBirth,
            FirstName = firstName,
            MiddleName = null,
            LastName = lastName,
            EmailAddresses = new[] { email },
            HasActiveSanctions = false,
            NationalInsuranceNumber = null,
            Trn = trn,
            Uid = Guid.NewGuid().ToString()
        });

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.RegisterFromLandingPage();

        await page.SubmitRegisterEmailPage(email);

        await page.SubmitRegisterEmailConfirmationPage();

        await page.SubmitRegisterPhonePage(mobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitRegisterNamePage(firstName, lastName);

        await page.SubmitRegisterPreferredNamePage();

        await page.SubmitDateOfBirthPage(dateOfBirth);

        await page.SubmitCheckAnswersPage();
    }

    private async Task ClearCookiesForTestClient(IBrowserContext context)
    {
        var cookies = await context.CookiesAsync();

        await context.ClearCookiesAsync();

        // All the Auth server cookies start with 'tis-'
        await context.AddCookiesAsync(
            cookies
                .Where(c => c.Name.StartsWith("tis-"))
                .Select(c => new Cookie()
                {
                    Domain = c.Domain,
                    Expires = c.Expires,
                    HttpOnly = c.HttpOnly,
                    Name = c.Name,
                    Path = c.Path,
                    SameSite = c.SameSite,
                    Secure = c.Secure,
                    Value = c.Value
                }));
    }

    private async Task<TrnTokenModel> CreateValidTrnToken(
        string? firstName = null,
        string? middleName = null,
        string? lastName = null,
        DateOnly? dateOfBirth = null,
        string? trn = null,
        string? email = null)
    {
        firstName ??= Faker.Name.First();
        middleName ??= Faker.Name.Middle();
        lastName ??= Faker.Name.Last();
        dateOfBirth ??= DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        trn ??= _hostFixture.TestData.GenerateTrn();
        email ??= _hostFixture.TestData.GenerateUniqueEmail();

        _hostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = dateOfBirth.Value,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Trn = trn,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                PendingDateOfBirthChange = false,
                PendingNameChange = false,
                Email = email
            });

        return await _hostFixture.TestData.GenerateTrnToken(trn, email: email);
    }

    private void ConfigureDqtApiFindTeachersRequest(FindTeachersResponseResult? result)
    {
        var results = result is not null ? new[] { result } : Array.Empty<FindTeachersResponseResult>();

        _hostFixture.DqtApiClient
            .Setup(mock => mock.FindTeachers(It.IsAny<FindTeachersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FindTeachersResponse()
            {
                Results = results
            });
    }
}
