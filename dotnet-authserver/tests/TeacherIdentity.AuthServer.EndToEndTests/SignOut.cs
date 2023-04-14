using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class SignOut : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public SignOut(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
        _hostFixture.OnTestStarting();
    }

    [Fact]
    public async Task SignOutFromClient()
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

        await page.SignOutFromTestClient();

        await page.AssertSignedOutOnTestClient();

        _hostFixture.EventObserver.AssertEventsSaved(
            e => Assert.IsType<Events.UserSignedInEvent>(e),
            e =>
            {
                var userSignedOut = Assert.IsType<Events.UserSignedOutEvent>(e);
                Assert.Equal(DateTime.UtcNow, userSignedOut.CreatedUtc, TimeSpan.FromSeconds(10));
                Assert.Equal(user.UserId, userSignedOut.User.UserId);
                Assert.Equal(_hostFixture.TestClientId, userSignedOut.ClientId);
            });
    }

    [Fact]
    public async Task SignOutFromIdDirectly()
    {
        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.GoToAccountPage();

        await page.SignInFromLandingPage();

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SignOutFromAccountPageWithoutClientContext();

        _hostFixture.EventObserver.AssertEventsSaved(
            e => Assert.IsType<Events.UserSignedInEvent>(e),
            e =>
            {
                var userSignedOut = Assert.IsType<Events.UserSignedOutEvent>(e);
                Assert.Equal(DateTime.UtcNow, userSignedOut.CreatedUtc, TimeSpan.FromSeconds(10));
                Assert.Equal(user.UserId, userSignedOut.User.UserId);
                Assert.Null(userSignedOut.ClientId);
            });
    }

    [Fact]
    public async Task SignOutViaAccountPageWithClientContext()
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

        await page.SignOutFromAccountPageWithClientContext();

        await page.AssertSignedOutOnTestClient();

        _hostFixture.EventObserver.AssertEventsSaved(
            e => Assert.IsType<Events.UserSignedInEvent>(e),  // OAuth sign in with a client
            e => Assert.IsType<Events.UserSignedInEvent>(e),  // Account page sign in
            e =>
            {
                var userSignedOut = Assert.IsType<Events.UserSignedOutEvent>(e);
                Assert.Equal(DateTime.UtcNow, userSignedOut.CreatedUtc, TimeSpan.FromSeconds(10));
                Assert.Equal(user.UserId, userSignedOut.User.UserId);
                Assert.Equal(_hostFixture.TestClientId, userSignedOut.ClientId);
            });
    }
}
