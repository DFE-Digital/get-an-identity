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
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();

        var newFirstName = Faker.Name.First();
        var newLastName = Faker.Name.Last();

        {
            using var scope = _hostFixture.AuthServerServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();

            var userId = Guid.NewGuid();

            dbContext.Users.Add(new User()
            {
                Created = DateTime.UtcNow,
                EmailAddress = email,
                FirstName = firstName,
                LastName = lastName,
                UserId = userId,
                UserType = UserType.Default,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                CompletedTrnLookup = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                TrnLookupStatus = TrnLookupStatus.None
            });

            await dbContext.SaveChangesAsync();
        }

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Test client app

        await page.GotoAsync("/");
        await page.ClickAsync("text=Profile");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", email);
        await page.ClickAsync("button:has-text('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:has-text('Continue')");

        // Confirm your details page

        await page.WaitForSelectorAsync("h1:has-text('Your details')");
        await page.Locator("*:has(> dt:text('Name'))").GetByText("Change").ClickAsync();

        // Update your name page

        await page.WaitForSelectorAsync("h1:has-text('Update your name')");
        await page.FillAsync("text=First name", newFirstName);
        await page.FillAsync("text=Last name", newLastName);
        await page.ClickAsync("button:has-text('Continue')");

        // Confirm your details

        await page.WaitForSelectorAsync("h1:has-text('Your details')");
        await page.ClickAsync("button:has-text('Continue')");

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
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();

        var newEmail = Faker.Internet.Email();

        {
            using var scope = _hostFixture.AuthServerServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();

            var userId = Guid.NewGuid();

            dbContext.Users.Add(new User()
            {
                Created = DateTime.UtcNow,
                EmailAddress = email,
                FirstName = firstName,
                LastName = lastName,
                UserId = userId,
                UserType = UserType.Default,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                CompletedTrnLookup = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                TrnLookupStatus = TrnLookupStatus.None
            });

            await dbContext.SaveChangesAsync();
        }

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Test client app

        await page.GotoAsync("/");
        await page.ClickAsync("text=Profile");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", email);
        await page.ClickAsync("button:has-text('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:has-text('Continue')");

        // Confirm your details page

        await page.WaitForSelectorAsync("h1:has-text('Your details')");
        await page.Locator("*:has(> dt:text('Email address'))").GetByText("Change").ClickAsync();

        // Update your email page

        await page.WaitForSelectorAsync("h1:has-text('Change your email address')");
        await page.FillAsync("text=Enter your new email address", newEmail);
        await page.ClickAsync("button:has-text('Continue')");

        // Confirm your email address page

        await page.WaitForSelectorAsync("h1:has-text('Confirm your email address')");
        pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:has-text('Continue')");

        // Confirm your details page

        await page.WaitForSelectorAsync("h1:has-text('Your details')");
        await page.ClickAsync("button:has-text('Continue')");

        // Test Client app

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        Assert.Equal(newEmail, await page.InnerTextAsync("data-testid=email"));
    }
}
