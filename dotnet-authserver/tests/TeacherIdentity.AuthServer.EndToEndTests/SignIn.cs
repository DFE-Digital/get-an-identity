using Microsoft.Playwright;
using TeacherIdentity.AuthServer.Models;

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

        await ClearCookiesForTestClient();

        await page.StartOAuthJourney(additionalScope: null);

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user);

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
}
