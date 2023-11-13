using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class Elevate : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public Elevate(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
        _hostFixture.OnTestStarting();
    }

    [Fact]
    public async Task UserSignsInWithLowVerificationLevel_IsRedirectedToElevateJourneyAndCompletesSuccessfully()
    {
        var user = await _hostFixture.TestData.CreateUser(hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Low);
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        ConfigureDqtApiGetTeacherByTrnRequest(user.Trn!, new TeacherInfo()
        {
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            NationalInsuranceNumber = nino,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = user.Trn!,
            Alerts = Array.Empty<AlertInfo>(),
            AllowIdSignInWithProhibitions = false
        });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: CustomScopes.DqtRead, trnMatchPolicy: TrnMatchPolicy.Strict);

        await page.SignInFromLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitElevateLandingPage();

        await page.SubmitRegisterNiNumberPage(nino);

        await page.SubmitRegisterTrnPage(user.Trn!);

        ConfigureDqtApiFindTeachersRequest(new FindTeachersResponseResult()
        {
            DateOfBirth = user.DateOfBirth,
            EmailAddresses = new[] { user.EmailAddress },
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            LastName = user.LastName,
            HasActiveSanctions = false,
            NationalInsuranceNumber = nino,
            Trn = user.Trn!,
            Uid = Guid.NewGuid().ToString()
        });

        ConfigureDqtApiGetTeacherByTrnRequest(user.Trn!, new TeacherInfo()
        {
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            NationalInsuranceNumber = nino,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = user.Trn!,
            Alerts = Array.Empty<AlertInfo>(),
            AllowIdSignInWithProhibitions = false
        });

        await page.SubmitElevateCheckAnswersPage();

        await page.SubmitCompletePageForExistingUser();

        user = await _hostFixture.TestData.WithDbContext(dbContext => dbContext.Users.SingleAsync(u => u.UserId == user.UserId));
        await page.AssertSignedInOnTestClient(user, expectTrn: true, expectNiNumber: true);
    }

    [Theory]
    [InlineData(TrnRequirementType.Optional, true)]
    [InlineData(TrnRequirementType.Required, false)]
    public async Task UserSignsInWithLowVerificationLevel_IsRedirectedToElevateJourneyButTrnNotFound(
        TrnRequirementType trnRequirementType,
        bool expectContinueButtonOnCompletePage)
    {
        var user = await _hostFixture.TestData.CreateUser(hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Low);
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        ConfigureDqtApiGetTeacherByTrnRequest(user.Trn!, new()
        {
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            NationalInsuranceNumber = nino,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = user.Trn!,
            Alerts = Array.Empty<AlertInfo>(),
            AllowIdSignInWithProhibitions = false
        });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: CustomScopes.DqtRead, trnMatchPolicy: TrnMatchPolicy.Strict, trnRequirement: trnRequirementType);

        await page.SignInFromLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitElevateLandingPage();

        await page.SubmitRegisterNiNumberPage(nino);

        await page.SubmitRegisterTrnPage(user.Trn!);

        ConfigureDqtApiFindTeachersRequest(result: null);

        await page.SubmitElevateCheckAnswersPage();

        if (expectContinueButtonOnCompletePage)
        {
            await page.SubmitCompletePageForExistingUser();

            user = await _hostFixture.TestData.WithDbContext(dbContext => dbContext.Users.SingleAsync(u => u.UserId == user.UserId));
            await page.AssertSignedInOnTestClient(user, expectTrn: false, expectNiNumber: false);
        }
        else
        {
            await page.AssertOnCompletePageWithNoContinueButton();
        }
    }

    [Fact]
    public async Task AlreadySignedInUserWithLowVerificationLevel_IsRedirectedToElevateJourney()
    {
        var user = await _hostFixture.TestData.CreateUser(hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Low);
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        ConfigureDqtApiGetTeacherByTrnRequest(user.Trn!, new TeacherInfo()
        {
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            NationalInsuranceNumber = nino,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = user.Trn!,
            Alerts = Array.Empty<AlertInfo>(),
            AllowIdSignInWithProhibitions = false
        });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: CustomScopes.DqtRead, trnMatchPolicy: TrnMatchPolicy.Default);

        await page.SignInFromLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user, expectNiNumber: false);

        await context.ClearCookiesForTestClient();

        await page.StartOAuthJourney(additionalScope: CustomScopes.DqtRead, trnMatchPolicy: TrnMatchPolicy.Strict);

        await page.SubmitElevateLandingPage();

        await page.SubmitRegisterNiNumberPage(nino);

        await page.SubmitRegisterTrnPage(user.Trn!);

        ConfigureDqtApiFindTeachersRequest(new FindTeachersResponseResult()
        {
            DateOfBirth = user.DateOfBirth,
            EmailAddresses = new[] { user.EmailAddress },
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            LastName = user.LastName,
            HasActiveSanctions = false,
            NationalInsuranceNumber = nino,
            Trn = user.Trn!,
            Uid = Guid.NewGuid().ToString()
        });

        ConfigureDqtApiGetTeacherByTrnRequest(user.Trn!, new TeacherInfo()
        {
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            NationalInsuranceNumber = nino,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = user.Trn!,
            Alerts = Array.Empty<AlertInfo>(),
            AllowIdSignInWithProhibitions = false
        });

        await page.SubmitElevateCheckAnswersPage();

        await page.SubmitCompletePageForExistingUser();

        user = await _hostFixture.TestData.WithDbContext(dbContext => dbContext.Users.SingleAsync(u => u.UserId == user.UserId));
        await page.AssertSignedInOnTestClient(user, expectTrn: true, expectNiNumber: true);
    }

    [Fact]
    public async Task UserSignsInWithLowVerificationLevelAndHasProhibitions_IsShownBlockedPageForServiceThatBlocksProhitiedTeachers()
    {
        var user = await _hostFixture.TestData.CreateUser(hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Low);
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        ConfigureDqtApiGetTeacherByTrnRequest(user.Trn!, new TeacherInfo()
        {
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            NationalInsuranceNumber = nino,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = user.Trn!,
            Alerts = new[]
            {
                new AlertInfo() { AlertType = AlertType.Prohibition, DqtSanctionCode = "G1" }
            },
            AllowIdSignInWithProhibitions = false
        });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: CustomScopes.DqtRead, trnMatchPolicy: TrnMatchPolicy.Strict, blockProhibitedTeachers: true);

        await page.SignInFromLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.AssertOnBlockedPage();
    }

    [Fact]
    public async Task AlreadySignedInUserWithLowVerificationLevelHasProhibitions_IsShownBlockedPageForServiceThatBlocksProhitiedTeachers()
    {
        var user = await _hostFixture.TestData.CreateUser(hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Low);
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        ConfigureDqtApiGetTeacherByTrnRequest(user.Trn!, new TeacherInfo()
        {
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            NationalInsuranceNumber = nino,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = user.Trn!,
            Alerts = new[]
            {
                new AlertInfo() { AlertType = AlertType.Prohibition, DqtSanctionCode = "G1" }
            },
            AllowIdSignInWithProhibitions = false
        });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: CustomScopes.DqtRead, trnMatchPolicy: TrnMatchPolicy.Default);

        await page.SignInFromLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user, expectNiNumber: false);

        await context.ClearCookiesForTestClient();

        await page.StartOAuthJourney(additionalScope: CustomScopes.DqtRead, trnMatchPolicy: TrnMatchPolicy.Strict, blockProhibitedTeachers: true);

        await page.AssertOnBlockedPage();
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

    private void ConfigureDqtApiGetTeacherByTrnRequest(string trn, TeacherInfo? result)
    {
        _hostFixture.DqtApiClient
            .Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }
}
