using TeacherIdentity.AuthServer.Models;

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

        await page.GoToAccountPage();

        await page.SignInFromLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage(HostFixture.UserVerificationPin);

        await page.AssertOnAccountPage();
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

        await page.SubmitEmailConfirmationPage(HostFixture.UserVerificationPin);

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user);

        await page.GoToAccountPageFromTestClient();

        await page.ReturnToTestClientFromAccountPage();
    }
}
