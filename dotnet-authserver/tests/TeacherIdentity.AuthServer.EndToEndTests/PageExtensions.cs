using Microsoft.Playwright;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public static class PageExtensions
{
    public static async Task StartOAuthJourney(this IPage page, string? additionalScope = null)
    {
        await page.GotoAsync($"/profile?scope=email+openid+profile{(additionalScope is not null ? "+" + Uri.EscapeDataString(additionalScope) : string.Empty)}");
    }

    public static async Task AssertOnTestClient(this IPage page)
    {
        await page.WaitForURLAsync(url => url.StartsWith(HostFixture.ClientBaseUrl));
    }

    public static Task AssertSignedInOnTestClient(this IPage page, User user) =>
        AssertSignedInOnTestClient(page, user.EmailAddress, user.Trn, user.FirstName, user.LastName);

    public static async Task AssertSignedInOnTestClient(this IPage page, string email, string? trn, string firstName, string lastName)
    {
        await page.AssertOnTestClient();

        var signedInEmail = await page.InnerTextAsync("data-testid=email");
        Assert.Equal(email, signedInEmail);
        Assert.Equal(trn ?? string.Empty, await page.InnerTextAsync("data-testid=trn"));
        Assert.Equal(firstName, await page.InnerTextAsync("data-testid=first-name"));
        Assert.Equal(lastName, await page.InnerTextAsync("data-testid=last-name"));
    }

    public static async Task AssertSignedOutOnTestClient(this IPage page)
    {
        await page.AssertOnTestClient();

        var signedInMarker = await page.GetByTestId("SignedIn").InnerTextAsync();
        Assert.Equal(bool.FalseString, signedInMarker);
    }

    public static async Task SignInFromLandingPage(this IPage page)
    {
        await page.WaitForSelectorAsync("h1:text-is('Create a DfE Identity account')");
        await page.ClickAsync("a:text-is('Sign in')");
    }

    public static async Task SubmitEmailPage(this IPage page, string email)
    {
        await page.WaitForSelectorAsync(":text-is('Your email address')");
        await page.FillAsync("input[type='email']", email);
        await page.ClickAsync("button:text-is('Continue')");
    }

    public static async Task SubmitEmailConfirmationPage(this IPage page, string pin)
    {
        await page.WaitForSelectorAsync("h1:text-is('Confirm your email address')");
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");
    }

    public static async Task SubmitCompletePageForExistingUser(this IPage page)
    {
        Assert.Equal(1, await page.GetByText("Your details").CountAsync());
        await page.ClickAsync("button:text-is('Continue')");
    }

    public static async Task SignOutFromTestClient(this IPage page)
    {
        await page.ClickAsync("a:text-is('Sign out')");

        await page.WaitForURLAsync($"{HostFixture.ClientBaseUrl}/");
    }

    public static async Task GoToAccountPage(this IPage page)
    {
        await page.GotoAsync($"{HostFixture.AuthServerBaseUrl}/account");
    }

    public static async Task AssertOnAccountPage(this IPage page)
    {
        await page.WaitForSelectorAsync("h1:text-is('Your details')");
    }

    public static async Task SignOutFromAccountPageWithoutClientContext(this IPage page)
    {
        await page.AssertOnAccountPage();

        await page.RunAndWaitForResponseAsync(
            () => page.ClickAsync("a:text-is('Sign out')"),
            resp => resp.Url == $"{HostFixture.AuthServerBaseUrl}/" && resp.Status == 404);
    }

    public static async Task SignOutFromAccountPageWithClientContext(this IPage page)
    {
        await page.AssertOnAccountPage();

        await page.ClickAsync("a:text-is('Sign out')");

        await page.WaitForURLAsync($"{HostFixture.ClientBaseUrl}/");

        await page.AssertOnTestClient();
    }

    public static async Task GoToAccountPageFromTestClient(this IPage page)
    {
        await page.ClickAsync("a:text-is('DfE Identity account')");

        await page.AssertOnAccountPage();
    }

    public static async Task ReturnToTestClientFromAccountPage(this IPage page)
    {
        await page.AssertOnAccountPage();

        await page.ClickAsync("a:text-is('Back to Development test client')");

        await page.AssertOnTestClient();
    }
}
