using Microsoft.Playwright;
using Moq;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public partial class SignIn : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public SignIn(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
        _hostFixture.OnTestStarting();
    }

    [Theory]
    [InlineData(CustomScopes.DqtRead)]
#pragma warning disable CS0618 // Type or member is obsolete
    [InlineData(CustomScopes.Trn)]
#pragma warning restore CS0618 // Type or member is obsolete
    public async Task ExistingTeacherUser_CanSignInSuccessfullyWithEmailAndPin(string additionalScope)
    {
        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope);

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user);

        _hostFixture.EventObserver.AssertEventsSaved(e => AssertEventIsUserSignedIn(e, user.UserId));
    }

    [Fact]
    public async Task NewTeacherUser_WithFoundTrn_CreatesUserAndCompletesFlow()
    {
        var email = Faker.Internet.Email();
        var officialFirstName = Faker.Name.First();
        var officialLastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = _hostFixture.TestData.GenerateTrn();

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await SignInAsNewTeacherUserWithDqtReadScope(page, email, officialFirstName, officialLastName, trn, dateOfBirth);
    }

    [Fact]
    public async Task NewTeacherUser_WithoutFoundTrn_CreatesUserAndCompletesFlow()
    {
        var email = Faker.Internet.Email();
        var officialFirstName = Faker.Name.First();
        var officialLastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var nino = Faker.Identification.UkNationalInsuranceNumber();
        var ittProvider = Faker.Company.Name();

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

        await page.SubmitEmailPage(email);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitTrnBookendPage();

        await page.SubmitTrnHasTrnPageWithoutTrn();

        await page.SubmitTrnOfficialNamePage(officialFirstName, officialLastName);

        await page.SubmitTrnPreferredNamePageWithoutPreferredName();

        await page.SubmitTrnDateOfBirthPage(dateOfBirth);

        await page.SubmitTrnHasNinoPage(hasNino: true);

        await page.SubmitTrnNiNumberPage(nino);

        await page.SubmitTrnAwardedQtsPage(awardedQts: true);

        await page.SubmitTrnIttProviderPageWithIttProvider(ittProvider);

        await page.SubmitTrnCheckAnswersPage();

        await page.SubmitTrnNoMatchPage();

        await page.SubmitCompletePageForNewUserInLegacyTrnJourney();

        await page.AssertSignedInOnTestClient(email, trn: null, officialFirstName, officialLastName);
    }

    [Fact]
    public async Task ExistingTeacherUser_SignsInWithinSameSessionTheyRegisteredWith_SkipsEmailAndPinAndShowsCorrectConfirmationPage()
    {
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var trn = _hostFixture.TestData.GenerateTrn();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await SignInAsNewTeacherUserWithDqtReadScope(page, email, firstName, lastName, trn, dateOfBirth);

        await ClearCookiesForTestClient();

        await page.StartOAuthJourney(additionalScope: CustomScopes.DqtRead);

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(email, trn, firstName, lastName);

        async Task ClearCookiesForTestClient()
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
    }

    [Fact]
    public async Task NewTeacherUser_WithTrnMatchingExistingAccount_VerifiesExistingAccountEmailAndCanSignInSuccessfully()
    {
        var existingTrnOwner = await _hostFixture.TestData.CreateUser(hasTrn: true);

        var trn = existingTrnOwner.Trn!;
        var trnOwnerEmailAddress = existingTrnOwner.EmailAddress;
        var email = Faker.Internet.Email();
        var officialFirstName = Faker.Name.First();
        var officialLastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());

        ConfigureDqtApiFindTeachersRequest(result: null);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: CustomScopes.DqtRead);

        await page.SubmitEmailPage(email);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitTrnBookendPage();

        await page.SubmitTrnHasTrnPageWithTrn(trn);

        await page.SubmitTrnOfficialNamePage(officialFirstName, officialLastName);

        await page.SubmitTrnPreferredNamePageWithoutPreferredName();

        // Simulate DQT API returning result when next page submitted

        ConfigureDqtApiFindTeachersRequest(result: new()
        {
            DateOfBirth = dateOfBirth,
            FirstName = officialFirstName,
            LastName = officialLastName,
            EmailAddresses = new[] { email },
            HasActiveSanctions = false,
            NationalInsuranceNumber = null,
            Trn = trn,
            Uid = Guid.NewGuid().ToString()
        });

        await page.SubmitTrnDateOfBirthPage(dateOfBirth);

        await page.SubmitTrnCheckAnswersPage();

        await page.SubmitTrnTrnInUsePage();

        await page.SubmitTrnChooseEmailPage(trnOwnerEmailAddress);

        await page.SubmitCompletePageForNewUserInLegacyTrnJourney();

        await page.AssertSignedInOnTestClient(existingTrnOwner);

        _hostFixture.EventObserver.AssertEventsSaved(e => AssertEventIsUserSignedIn(e, existingTrnOwner.UserId));
    }

    [Fact]
    public async Task FirstRequestToProtectedAreaOfSiteForUserAlreadySignedInViaOAuth_IssuesUserSignedInEvent()
    {
        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: null);

        await page.SignInFromLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user);

        _hostFixture.EventObserver.Clear();

        await page.GoToAccountPage();

        _hostFixture.EventObserver.AssertEventsSaved(e => AssertEventIsUserSignedIn(e, user.UserId, expectOAuthProperties: false));
    }

    [Fact]
    public async Task TeacherUser_WithTrnAssignedViaApi_CanSignInSuccessfully()
    {
        var user = await _hostFixture.TestData.CreateUser(hasTrn: true, haveCompletedTrnLookup: false);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(CustomScopes.DqtRead);

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user);
    }

    private void AssertEventIsUserSignedIn(
        Events.EventBase @event,
        Guid userId,
        bool expectOAuthProperties = true)
    {
        var userSignedIn = Assert.IsType<Events.UserSignedInEvent>(@event);
        Assert.Equal(DateTime.UtcNow, userSignedIn.CreatedUtc, TimeSpan.FromSeconds(10));
        Assert.Equal(userId, userSignedIn.User.UserId);

        if (expectOAuthProperties)
        {
            Assert.Equal(_hostFixture.TestClientId, userSignedIn.ClientId);
            Assert.NotNull(userSignedIn.Scope);
        }
    }

    private async Task SignInAsNewTeacherUserWithDqtReadScope(
        IPage page,
        string email,
        string officialFirstName,
        string officialLastName,
        string trn,
        DateOnly dateOfBirth)
    {
        ConfigureDqtApiFindTeachersRequest(result: null);

        await page.StartOAuthJourney(additionalScope: CustomScopes.DqtRead);

        await page.SubmitEmailPage(email);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitTrnBookendPage();

        await page.SubmitTrnHasTrnPageWithTrn(trn);

        await page.SubmitTrnOfficialNamePage(officialFirstName, officialLastName);

        await page.SubmitTrnPreferredNamePageWithoutPreferredName();

        // Simulate DQT API returning result when next page submitted

        ConfigureDqtApiFindTeachersRequest(result: new()
        {
            DateOfBirth = dateOfBirth,
            FirstName = officialFirstName,
            LastName = officialLastName,
            EmailAddresses = new[] { email },
            HasActiveSanctions = false,
            NationalInsuranceNumber = null,
            Trn = trn,
            Uid = Guid.NewGuid().ToString()
        });

        await page.SubmitTrnDateOfBirthPage(dateOfBirth);

        await page.SubmitTrnCheckAnswersPage();

        await page.SubmitCompletePageForNewUserInLegacyTrnJourney();

        await page.AssertSignedInOnTestClient(email, trn, officialFirstName, officialLastName);
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
