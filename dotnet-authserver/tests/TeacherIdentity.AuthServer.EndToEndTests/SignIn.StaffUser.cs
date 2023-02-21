using Microsoft.Playwright;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public partial class SignIn
{
    [Fact]
    public async Task StaffUser_CanSignInSuccessfullyWithEmailAndPin()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await SignInExistingStaffUserWithTestClient(page);
    }

    [Fact]
    public async Task StaffUser_CanSignInToAdminPageSuccessfullyWithEmailAndPin()
    {
        var staffUser = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles: StaffRoles.All);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Try to access protected admin area on auth server

        await page.GotoAsync($"{HostFixture.AuthServerBaseUrl}/admin/staff");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", staffUser.EmailAddress);
        await page.ClickAsync("button:text-is('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be on the originally request URL, /admin/staff

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        Assert.EndsWith("/admin/staff", page.Url);

        // Check events have been emitted

        _hostFixture.EventObserver.AssertEventsSaved(e => AssertEventIsUserSignedIn(e, staffUser.UserId, expectOAuthProperties: false));
    }

    [Fact]
    public async Task StaffUser_MissingPermission_GetsForbiddenError()
    {
        var staffUser = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles: Array.Empty<string>());

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Start on the client app and try to access a protected area with admin scope

        await page.GotoAsync($"/profile?scope={CustomScopes.UserRead}");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", staffUser.EmailAddress);
        await page.ClickAsync("button:text-is('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Should get a Forbidden error

        await page.WaitForSelectorAsync("h1:text-is('Forbidden')");
    }

    private async Task<Guid> SignInExistingStaffUserWithTestClient(IPage page)
    {
        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles: StaffRoles.All);

        // Start on the client app and try to access a protected area with admin scope

        await page.GotoAsync($"/profile?scope=email+openid+profile+{CustomScopes.UserRead}");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", user.EmailAddress);
        await page.ClickAsync("button:text-is('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be on the confirmation page

        Assert.Equal(1, await page.Locator("data-testid=known-user-content").CountAsync());
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be back at the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        var signedInEmail = await page.InnerTextAsync("data-testid=email");
        Assert.Equal(string.Empty, await page.InnerTextAsync("data-testid=trn"));
        Assert.Equal(user.EmailAddress, signedInEmail);

        // Check events have been emitted

        _hostFixture.EventObserver.AssertEventsSaved(e => AssertEventIsUserSignedIn(e, user.UserId));

        return user.UserId;
    }
}
