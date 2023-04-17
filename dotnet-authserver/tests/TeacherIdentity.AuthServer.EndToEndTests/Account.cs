using Microsoft.Playwright;
using Moq;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class Account : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public Account(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
        _hostFixture.OnTestStarting();
    }

    [Fact]
    public async Task AccessAccountPageDirectlyWithoutClientSignIn()
    {
        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await SignInToAccountPage(page, user);
    }

    [Fact]
    public async Task AccessAccountPageFromClient()
    {
        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney();

        await page.SignInFromLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user);

        await page.GoToAccountPageFromTestClient();

        await page.ReturnToTestClientFromAccountPage();
    }

    [Fact]
    public async Task ChangeOfficialName()
    {
        var user = await _hostFixture.TestData.CreateUser(hasTrn: true);

        var dqtTeacherInfo = new TeacherInfo()
        {
            DateOfBirth = user.DateOfBirth!.Value.AddDays(1), // Must be different to ID DOB
            FirstName = user.FirstName,
            MiddleName = "",
            LastName = user.LastName,
            NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = user.Trn!
        };

        ConfigureDqtApiGetTeacherResponse(user.Trn!, dqtTeacherInfo);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await SignInToAccountPage(page, user);

        Assert.True(await page.GetByTestId("dob-conflict-notification-banner").IsVisibleAsync());
        Assert.False(await page.GetByTestId("dob-pending-review-tag").IsVisibleAsync());
        Assert.False(await page.GetByTestId("dqt-dob-pending-review-tag").IsVisibleAsync());

        await page.ClickChangeLinkForElementWithTestId("dqt-dob-change-link");

        await page.WaitForUrlPathAsync("/account/official-date-of-birth");
        await page.ClickContinueButton();

        await page.WaitForUrlPathAsync("/account/official-date-of-birth/details");
        await page.FillDateInput(user.DateOfBirth!.Value);
        await page.ClickContinueButton();

        await page.WaitForUrlPathAsync("/account/official-date-of-birth/evidence");
        await page.SetInputFilesAsync(
            "text=Upload a file",
            new FilePayload()
            {
                Name = "evidence.jpg",
                MimeType = "image/jpeg",
                Buffer = TestData.JpegImage
            });
        await page.ClickContinueButton();

        await page.WaitForUrlPathAsync("/account/official-date-of-birth/confirm");
        await page.ClickAsync("button:text-is('Submit change')");

        ConfigureDqtApiGetTeacherResponse(
            user.Trn!,
            dqtTeacherInfo with
            {
                PendingDateOfBirthChange = true
            });

        await page.WaitForUrlPathAsync("/account");
        await page.ReloadAsync();  // Ensure the page has observed the re-configured DQT API response above

        Assert.False(await page.GetByTestId("dob-conflict-notification-banner").IsVisibleAsync());
        Assert.True(await page.GetByTestId("dob-pending-review-tag").IsVisibleAsync());
        Assert.True(await page.GetByTestId("dqt-dob-pending-review-tag").IsVisibleAsync());
        Assert.False(await page.GetByTestId("dob-change-link").IsVisibleAsync());
        Assert.False(await page.GetByTestId("dqt-dob-change-link").IsVisibleAsync());

        ConfigureDqtApiGetTeacherResponse(
            user.Trn!,
            dqtTeacherInfo with
            {
                DateOfBirth = user.DateOfBirth!.Value,
                PendingDateOfBirthChange = false
            });

        await page.ReloadAsync();

        Assert.False(await page.GetByTestId("dob-conflict-notification-banner").IsVisibleAsync());
        Assert.False(await page.GetByTestId("dob-pending-review-tag").IsVisibleAsync());
        Assert.False(await page.GetByTestId("dqt-dob-pending-review-tag").IsVisibleAsync());
        Assert.False(await page.GetByTestId("dob-change-link").IsVisibleAsync());
        Assert.False(await page.GetByTestId("dqt-dob-change-link").IsVisibleAsync());
    }

    private async Task SignInToAccountPage(IPage page, User user)
    {
        await page.GoToAccountPage();

        await page.SignInFromLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.AssertOnAccountPage();
    }

    private void ConfigureDqtApiGetTeacherResponse(string trn, TeacherInfo teacherInfo)
    {
        _hostFixture.DqtApiClient
            .Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teacherInfo);
    }
}
