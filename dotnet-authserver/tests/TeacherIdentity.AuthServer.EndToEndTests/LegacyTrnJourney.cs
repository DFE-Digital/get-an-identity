using Microsoft.Playwright;
using Moq;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class LegacyTrnJourney : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public LegacyTrnJourney(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
        _hostFixture.OnTestStarting();
    }

    [Fact]
    public async Task ExistingTeacherUser_CanSignInSuccessfullyWithEmailAndPin()
    {
        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(CustomScopes.Trn, TrnRequirementType.Legacy);

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user);

        _hostFixture.EventObserver.AssertEventsSaved(
            e => _hostFixture.AssertEventIsUserSignedIn(e, user.UserId));
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

        await SignInAsNewTeacherUserWithTrnScope(page, email, officialFirstName, officialLastName, trn, dateOfBirth);
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

        await page.StartOAuthJourney(CustomScopes.Trn, TrnRequirementType.Legacy);

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

        await page.StartOAuthJourney(CustomScopes.Trn, TrnRequirementType.Legacy);

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

        _hostFixture.EventObserver.AssertEventsSaved(
            e => _hostFixture.AssertEventIsUserSignedIn(e, existingTrnOwner.UserId));
    }

    [Fact]
    public async Task TeacherUser_WithTrnAssignedViaApi_CanSignInSuccessfully()
    {
        var user = await _hostFixture.TestData.CreateUser(hasTrn: true, haveCompletedTrnLookup: false);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(CustomScopes.Trn, TrnRequirementType.Legacy);

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user);
    }

    private async Task SignInAsNewTeacherUserWithTrnScope(
        IPage page,
        string email,
        string officialFirstName,
        string officialLastName,
        string trn,
        DateOnly dateOfBirth)
    {
        ConfigureDqtApiFindTeachersRequest(result: null);

        await page.StartOAuthJourney(CustomScopes.Trn, TrnRequirementType.Legacy);

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
