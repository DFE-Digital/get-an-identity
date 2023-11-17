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

    [Fact]
    public async Task ExistingTeacherUser_AlreadySignedIn_SkipsEmailAndPinAndShowsCorrectConfirmationPage()
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

        await context.ClearCookiesForTestClient();

        await page.StartOAuthJourney(additionalScope: null);

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user);
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

        _hostFixture.EventObserver.AssertEventsSaved(
            e => _hostFixture.AssertEventIsUserSignedIn(e, user.UserId, expectOAuthProperties: false));
    }

    [Fact]
    public async Task ExistingUserWithTrn_IsShownBlockedPageForServiceThatBlocksProhitiedTeachers()
    {
        var user = await _hostFixture.TestData.CreateUser(hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Medium);

        ConfigureDqtApiGetTeacherByTrnRequest(user.Trn!, new TeacherInfo()
        {
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            NationalInsuranceNumber = user.NationalInsuranceNumber,
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

        await page.StartOAuthJourney(additionalScope: CustomScopes.DqtRead, trnRequirement: TrnRequirementType.Required, trnMatchPolicy: TrnMatchPolicy.Strict, blockProhibitedTeachers: true);

        await page.SignInFromLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.AssertOnBlockedPage();
    }

    private void ConfigureDqtApiGetTeacherByTrnRequest(string trn, TeacherInfo? result)
    {
        _hostFixture.DqtApiClient
            .Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }
}
