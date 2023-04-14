using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class UpdateDetails : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public UpdateDetails(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
    }

    [Fact]
    public async Task UpdateNameWithinOAuthFlow()
    {
        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        var newFirstName = Faker.Name.First();
        var newLastName = Faker.Name.Last();

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Test client app

        await page.GotoAsync("/");
        await page.ClickAsync("text=Profile");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", user.EmailAddress);
        await page.ClickAsync("button:text-is('Continue')");

        var pin = HostFixture.UserVerificationPin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Confirm your details page

        await page.WaitForSelectorAsync("h1:text-is('Your details')");
        await page.Locator("*:has(> dt:text('Name'))").GetByText("Change").ClickAsync();

        // Update your name page

        await page.WaitForSelectorAsync("h1:text-is('Update your name')");
        await page.FillAsync("text=First name", newFirstName);
        await page.FillAsync("text=Last name", newLastName);
        await page.ClickAsync("button:text-is('Continue')");

        // Confirm your details

        await page.WaitForSelectorAsync("h1:text-is('Your details')");
        await page.ClickAsync("button:text-is('Continue')");

        // Test Client app

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        Assert.Equal(newFirstName, await page.InnerTextAsync("data-testid=first-name"));
        Assert.Equal(newLastName, await page.InnerTextAsync("data-testid=last-name"));
    }

    [Fact]
    public async Task UpdateEmailWithinOAuthFlow()
    {
        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        var newEmail = Faker.Internet.Email();

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Test client app

        await page.GotoAsync("/");
        await page.ClickAsync("text=Profile");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", user.EmailAddress);
        await page.ClickAsync("button:text-is('Continue')");

        var pin = HostFixture.UserVerificationPin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Confirm your details page

        await page.WaitForSelectorAsync("h1:text-is('Your details')");
        await page.Locator("*:has(> dt:text('Email address'))").GetByText("Change").ClickAsync();

        // Update your email page

        await page.WaitForSelectorAsync("h1:text-is('Change your email address')");
        await page.FillAsync("text=Enter your new email address", newEmail);
        await page.ClickAsync("button:text-is('Continue')");

        // Confirm your email address page

        await page.WaitForSelectorAsync("h1:text-is('Confirm your email address')");
        pin = HostFixture.UserVerificationPin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Confirm your details page

        await page.WaitForSelectorAsync("h1:text-is('Your details')");
        await page.ClickAsync("button:text-is('Continue')");

        // Test Client app

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        Assert.Equal(newEmail, await page.InnerTextAsync("data-testid=email"));
    }
}
