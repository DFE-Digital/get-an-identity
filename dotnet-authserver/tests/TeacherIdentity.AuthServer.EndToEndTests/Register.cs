using Moq;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class Register : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public Register(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
        _hostFixture.OnTestStarting();
    }

    [Fact]
    public async Task NewUser_WithoutTrnRequired_CanRegister()
    {
        var email = Faker.Internet.Email();
        var mobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: null);

        await page.RegisterFromLandingPage();

        await page.SubmitRegisterEmailPage(email);

        await page.SubmitRegisterEmailConfirmationPage();

        await page.SubmitRegisterPhonePage(mobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitRegisterNamePage(firstName, lastName);

        await page.SubmitDateOfBirthPage(dateOfBirth);

        await page.SubmitCheckAnswersPage();

        await page.SubmitCompletePageForNewUser();

        await page.AssertSignedInOnTestClient(email, trn: null, firstName, lastName);

        Guid createdUserId = default;

        _hostFixture.EventObserver.AssertEventsSaved(
            e =>
            {
                var userRegisteredEvent = Assert.IsType<UserRegisteredEvent>(e);
                Assert.Equal(_hostFixture.TestClientId, userRegisteredEvent.ClientId);
                Assert.Equal(email, userRegisteredEvent.User.EmailAddress);
                Assert.Equal(mobileNumber, userRegisteredEvent.User.MobileNumber);
                Assert.Equal(firstName, userRegisteredEvent.User.FirstName);
                Assert.Equal(lastName, userRegisteredEvent.User.LastName);
                Assert.Equal(dateOfBirth, userRegisteredEvent.User.DateOfBirth);

                createdUserId = userRegisteredEvent.User.UserId;
            },
            e =>
            {
                var userSignedInEvent = Assert.IsType<UserSignedInEvent>(e);
                Assert.Equal(createdUserId, userSignedInEvent.User.UserId);
            });
    }

    [Fact]
    public async Task NewUser_WithTrnRequired_TrnNotFound_CanRegister()
    {
        var email = Faker.Internet.Email();
        var mobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var nino = Faker.Identification.UkNationalInsuranceNumber();
        var ittProvider = Faker.Company.Name();
        var trn = _hostFixture.TestData.GenerateTrn();

        ConfigureDqtApiFindTeachersRequest(result: null);

        _hostFixture.DqtApiClient
            .Setup(mock => mock.GetIttProviders(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetIttProvidersResponse()
            {
                IttProviders = new[]
                {
                    new IttProvider()
                    {
                        ProviderName = "Provider 1",
                        Ukprn = "123"
                    },
                    new IttProvider()
                    {
                        ProviderName = ittProvider,
                        Ukprn = "234"
                    }
                }
            });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: CustomScopes.DqtRead);

        await page.RegisterFromLandingPage();

        await page.SubmitRegisterEmailPage(email);

        await page.SubmitRegisterEmailConfirmationPage();

        await page.SubmitRegisterPhonePage(mobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitRegisterNamePage(firstName, lastName);

        await page.SubmitDateOfBirthPage(dateOfBirth);

        await page.SubmitRegisterHasNinoPage(hasNino: true);

        await page.SubmitRegisterNiNumberPage(nino);

        await page.SubmitRegisterHasTrnPage(hasTrn: true);

        await page.SubmitRegisterTrnPage(trn);

        await page.SubmitRegisterHasQtsPage(hasQts: true);

        await page.SubmitRegisterIttProviderPageWithIttProvider(ittProvider);

        await page.SubmitCheckAnswersPage();

        await page.SubmitCompletePageForNewUser();

        await page.AssertSignedInOnTestClient(email, trn: null, firstName, lastName);

        Guid createdUserId = default;

        _hostFixture.EventObserver.AssertEventsSaved(
            e =>
            {
                var userRegisteredEvent = Assert.IsType<UserRegisteredEvent>(e);
                Assert.Equal(_hostFixture.TestClientId, userRegisteredEvent.ClientId);
                Assert.Equal(email, userRegisteredEvent.User.EmailAddress);
                Assert.Equal(mobileNumber, userRegisteredEvent.User.MobileNumber);
                Assert.Equal(firstName, userRegisteredEvent.User.FirstName);
                Assert.Equal(lastName, userRegisteredEvent.User.LastName);
                Assert.Equal(dateOfBirth, userRegisteredEvent.User.DateOfBirth);

                createdUserId = userRegisteredEvent.User.UserId;
            },
            e =>
            {
                var userSignedInEvent = Assert.IsType<UserSignedInEvent>(e);
                Assert.Equal(createdUserId, userSignedInEvent.User.UserId);
            });
    }

    [Fact]
    public async Task NewUser_WithTrnRequired_TrnFound_CanRegister()
    {
        var email = Faker.Internet.Email();
        var mobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = _hostFixture.TestData.GenerateTrn();

        ConfigureDqtApiFindTeachersRequest(result: new()
        {
            DateOfBirth = dateOfBirth,
            FirstName = firstName,
            LastName = lastName,
            EmailAddresses = new[] { email },
            HasActiveSanctions = false,
            NationalInsuranceNumber = null,
            Trn = trn,
            Uid = Guid.NewGuid().ToString()
        });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: CustomScopes.DqtRead);

        await page.RegisterFromLandingPage();

        await page.SubmitRegisterEmailPage(email);

        await page.SubmitRegisterEmailConfirmationPage();

        await page.SubmitRegisterPhonePage(mobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitRegisterNamePage(firstName, lastName);

        await page.SubmitDateOfBirthPage(dateOfBirth);

        await page.SubmitCheckAnswersPage();

        await page.SubmitCompletePageForNewUser();

        await page.AssertSignedInOnTestClient(email, trn, firstName, lastName);

        Guid createdUserId = default;

        _hostFixture.EventObserver.AssertEventsSaved(
            e =>
            {
                var userRegisteredEvent = Assert.IsType<UserRegisteredEvent>(e);
                Assert.Equal(_hostFixture.TestClientId, userRegisteredEvent.ClientId);
                Assert.Equal(email, userRegisteredEvent.User.EmailAddress);
                Assert.Equal(mobileNumber, userRegisteredEvent.User.MobileNumber);
                Assert.Equal(firstName, userRegisteredEvent.User.FirstName);
                Assert.Equal(lastName, userRegisteredEvent.User.LastName);
                Assert.Equal(dateOfBirth, userRegisteredEvent.User.DateOfBirth);

                createdUserId = userRegisteredEvent.User.UserId;
            },
            e =>
            {
                var userSignedInEvent = Assert.IsType<UserSignedInEvent>(e);
                Assert.Equal(createdUserId, userSignedInEvent.User.UserId);
            });
    }

    [Fact]
    public async Task NewUser_WithTrnRequired_MatchingExistingAccount_VerifiesExistingAccountEmailAndCanSignInSuccessfully()
    {
        var existingTrnOwner = await _hostFixture.TestData.CreateUser(hasTrn: true);

        var trn = existingTrnOwner.Trn!;
        var trnOwnerEmailAddress = existingTrnOwner.EmailAddress;
        var email = Faker.Internet.Email();
        var mobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());

        ConfigureDqtApiFindTeachersRequest(result: new()
        {
            DateOfBirth = dateOfBirth,
            FirstName = firstName,
            LastName = lastName,
            EmailAddresses = new[] { email },
            HasActiveSanctions = false,
            NationalInsuranceNumber = null,
            Trn = trn,
            Uid = Guid.NewGuid().ToString()
        });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: CustomScopes.DqtRead);

        await page.RegisterFromLandingPage();

        await page.SubmitRegisterEmailPage(email);

        await page.SubmitRegisterEmailConfirmationPage();

        await page.SubmitRegisterPhonePage(mobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitRegisterNamePage(firstName, lastName);

        await page.SubmitDateOfBirthPage(dateOfBirth);

        await page.SubmitCheckAnswersPage();

        await page.SubmitTrnTrnInUsePage();

        await page.SubmitTrnChooseEmailPage(trnOwnerEmailAddress);

        await page.SubmitCompletePageForNewUser();

        await page.AssertSignedInOnTestClient(existingTrnOwner);

        _hostFixture.EventObserver.AssertEventsSaved(
            e => _hostFixture.AssertEventIsUserSignedIn(e, existingTrnOwner.UserId));
    }

    [Fact]
    public async Task User_WithEmailAlreadyExists_SignsInExistingUser()
    {
        var existingUser = await _hostFixture.TestData.CreateUser();

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: null);

        await page.RegisterFromLandingPage();

        await page.SubmitRegisterEmailPage(existingUser.EmailAddress);

        await page.SubmitRegisterEmailConfirmationPage();

        await page.SignInFromRegisterEmailExistsPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(existingUser);

        _hostFixture.EventObserver.AssertEventsSaved(
            e => _hostFixture.AssertEventIsUserSignedIn(e, existingUser.UserId, expectOAuthProperties: true));
    }

    [Fact]
    public async Task User_WithMobileAlreadyExists_SignsInExistingUser()
    {
        var email = Faker.Internet.Email();
        var existingUser = await _hostFixture.TestData.CreateUser(hasMobileNumber: true);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: null);

        await page.RegisterFromLandingPage();

        await page.SubmitRegisterEmailPage(email);

        await page.SubmitRegisterEmailConfirmationPage();

        await page.SubmitRegisterPhonePage(existingUser.MobileNumber!);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SignInFromRegisterPhoneExistsPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(existingUser);

        _hostFixture.EventObserver.AssertEventsSaved(
            e => _hostFixture.AssertEventIsUserSignedIn(e, existingUser.UserId, expectOAuthProperties: true));
    }

    [Fact]
    public async Task User_WithMatchingNameAndDob_SignsInExistingUserFromEmail()
    {
        var email = Faker.Internet.Email();
        var mobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();

        var existingUser = await _hostFixture.TestData.CreateUser();

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: null);

        await page.RegisterFromLandingPage();

        await page.SubmitRegisterEmailPage(email);

        await page.SubmitRegisterEmailConfirmationPage();

        await page.SubmitRegisterPhonePage(mobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitRegisterNamePage(existingUser.FirstName, existingUser.LastName);

        await page.SubmitDateOfBirthPage(existingUser.DateOfBirth!.Value);

        await page.SubmitAccountExistsPage(isUsersAccount: true);

        await page.SubmitExistingAccountEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(existingUser);

        _hostFixture.EventObserver.AssertEventsSaved(
            e => _hostFixture.AssertEventIsUserSignedIn(e, existingUser.UserId, expectOAuthProperties: true));
    }

    [Fact]
    public async Task User_WithMatchingNameAndDob_SignsInExistingUserFromMobilePhone()
    {
        var email = Faker.Internet.Email();
        var mobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();

        var existingUser = await _hostFixture.TestData.CreateUser();

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: null);

        await page.RegisterFromLandingPage();

        await page.SubmitRegisterEmailPage(email);

        await page.SubmitRegisterEmailConfirmationPage();

        await page.SubmitRegisterPhonePage(mobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitRegisterNamePage(existingUser.FirstName, existingUser.LastName);

        await page.SubmitDateOfBirthPage(existingUser.DateOfBirth!.Value);

        await page.SubmitAccountExistsPage(isUsersAccount: true);

        await page.SubmitExistingAccountEmailConfirmationPage(cantAccessEmail: true);

        await page.SubmitExistingAccountPhonePage();

        await page.SubmitExistingAccountPhoneConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(existingUser);

        _hostFixture.EventObserver.AssertEventsSaved(
            e => _hostFixture.AssertEventIsUserSignedIn(e, existingUser.UserId, expectOAuthProperties: true));
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
