using FakeItEasy;
using Microsoft.Playwright;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class SignIn : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public SignIn(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
        _hostFixture.ResetMocks();
    }

    [Fact]
    public async Task ExistingTeacherUser_CanSignInSuccessfullyWithEmailAndPin()
    {
        var email = "joe.bloggs+existing-user@example.com";
        var trn = "1234567";

        {
            using var scope = _hostFixture.AuthServerServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();

            var userId = Guid.NewGuid();

            dbContext.Users.Add(new User()
            {
                Created = DateTime.UtcNow,
                EmailAddress = email,
                FirstName = "Joe",
                LastName = "Bloggs",
                UserId = userId,
                UserType = UserType.Default,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                Trn = trn,
                CompletedTrnLookup = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Start on the client app and try to access a protected area

        await page.GotoAsync("/");
        await page.ClickAsync("text=Profile");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Email", email);
        await page.ClickAsync("button:has-text('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:has-text('Continue')");

        // Should now be on the confirmation page

        Assert.Equal(1, await page.Locator("data-testid=known-user-content").CountAsync());
        await page.ClickAsync("button:has-text('Continue')");

        // Should now be back at the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        var signedInEmail = await page.InnerTextAsync("data-testid=email");
        Assert.Equal(trn ?? string.Empty, await page.InnerTextAsync("data-testid=trn"));
        Assert.Equal(email, signedInEmail);
    }

    [Fact]
    public async Task AdminUser_CanSignInSuccessfullyWithEmailAndPin()
    {
        var email = "admin.user@example.com";

        {
            using var scope = _hostFixture.AuthServerServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();

            var userId = Guid.NewGuid();

            dbContext.Users.Add(new User()
            {
                EmailAddress = email,
                FirstName = "Joe",
                LastName = "Bloggs",
                UserId = userId,
                UserType = UserType.Staff
            });

            await dbContext.SaveChangesAsync();
        }

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Start on the client app and try to access a protected area with admin scope

        await page.GotoAsync("/profile?scope=get-an-identity:admin");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Email", email);
        await page.ClickAsync("button:has-text('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:has-text('Continue')");

        // Should now be on the confirmation page

        Assert.Equal(1, await page.Locator("data-testid=known-user-content").CountAsync());
        await page.ClickAsync("button:has-text('Continue')");

        // Should now be back at the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        var signedInEmail = await page.InnerTextAsync("data-testid=email");
        Assert.Equal(string.Empty, await page.InnerTextAsync("data-testid=trn"));
        Assert.Equal(email, signedInEmail);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NewTeacherUser_IsRedirectedToFindAndRegistersAnAccountOnCallback(bool hasTrn)
    {
        var email = $"joe.bloggs+new-user-{(hasTrn ? "with-trn" : "without-trn")}@example.com";
        var firstName = "Joe";
        var lastName = "Bloggs";
        var trn = hasTrn ? "2345678" : null;
        var dateOfBirth = new DateOnly(1990, 1, 2);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await SignInAsNewTeacherUser(page, email, firstName, lastName, trn, dateOfBirth);
    }

    [Fact]
    public async Task ExistingTeacherUser_SignsInWithinSameSessionTheyRegisteredWith_SkipsEmailAndPinAndShowsCorrectConfirmationPage()
    {
        var email = $"joe.bloggs+existing-subsequent-sign-in@example.com";
        var firstName = "Joe";
        var lastName = "Bloggs";
        var trn = (string?)null;
        var dateOfBirth = new DateOnly(1990, 1, 2);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await SignInAsNewTeacherUser(page, email, firstName, lastName, trn, dateOfBirth);

        await ClearCookiesForTestClient();

        // Start on the client app and try to access a protected area

        await page.GotoAsync("/");
        await page.ClickAsync("text=Profile");

        // Should have jumped straight to confirmation page as the auth server knows who we are

        Assert.Equal(1, await page.Locator("data-testid=known-user-content").CountAsync());
        await page.ClickAsync("button:has-text('Continue')");

        // Should now be back at the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        var signedInEmail = await page.InnerTextAsync("data-testid=email");
        Assert.Equal(trn ?? string.Empty, await page.InnerTextAsync("data-testid=trn"));
        Assert.Equal(email, signedInEmail);

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
    public async Task NewTeacherUser_WithTrnMatchingExistingAccount_VerifiesExistingAccountEmailAndCanSignInSuccessfully()
    {
        var email = $"joe.bloggs+new-user-with-existing-trn@example.com";
        var firstName = "Joe";
        var lastName = "Bloggs";
        var trn = "3456789";
        var dateOfBirth = new DateOnly(1990, 1, 2);
        var trnOwnerEmailAddress = Faker.Internet.Email();

        {
            using var scope = _hostFixture.AuthServerServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();

            var userId = Guid.NewGuid();

            dbContext.Users.Add(new User()
            {
                Created = DateTime.UtcNow,
                EmailAddress = trnOwnerEmailAddress,
                FirstName = "Joe",
                LastName = "Bloggs",
                UserId = userId,
                UserType = UserType.Default,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                Trn = trn,
                CompletedTrnLookup = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Start on the client app and try to access a protected area

        await page.GotoAsync("/");
        await page.ClickAsync("text=Profile");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Email", email);
        await page.ClickAsync("button:has-text('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:has-text('Continue')");

        // Should now be at the first bookend page

        var urlPath = new Uri(page.Url).LocalPath;
        Assert.EndsWith("/trn", urlPath);

        await page.ClickAsync("button:has-text('Continue')");

        // Should now be on our stub Find page

        urlPath = new Uri(page.Url).LocalPath;
        Assert.EndsWith("/FindALostTrn", urlPath);

        await page.FillAsync("#FirstName", firstName);
        await page.FillAsync("#LastName", lastName);
        await page.FillAsync("#FirstName", firstName);
        await page.FillAsync("id=DateOfBirth.Day", dateOfBirth.Day.ToString());
        await page.FillAsync("id=DateOfBirth.Month", dateOfBirth.Month.ToString());
        await page.FillAsync("id=DateOfBirth.Year", dateOfBirth.Year.ToString());
        await page.FillAsync("#Trn", trn ?? string.Empty);

        await page.ClickAsync("button:has-text('Continue')");

        // Should now be on 'TRN in use' page

        pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);

        await page.ClickAsync("button:has-text('Continue')");

        // Should now be on 'Choose email' page

        await page.ClickAsync($"text={trnOwnerEmailAddress}");
        await page.ClickAsync("button:has-text('Continue')");

        // Should now be on the confirmation page

        Assert.Equal(1, await page.Locator("data-testid=first-time-user-content").CountAsync());
        await page.ClickAsync("button:has-text('Continue')");

        // Should now be back on the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        Assert.Equal(firstName, await page.InnerTextAsync("data-testid=first-name"));
        Assert.Equal(lastName, await page.InnerTextAsync("data-testid=last-name"));
        Assert.Equal(trnOwnerEmailAddress, await page.InnerTextAsync("data-testid=email"));
        Assert.Equal(trn ?? string.Empty, await page.InnerTextAsync("data-testid=trn"));
    }

    private async Task SignInAsNewTeacherUser(
        IPage page,
        string email,
        string firstName,
        string lastName,
        string? trn,
        DateOnly dateOfBirth)
    {
        // Start on the client app and try to access a protected area

        await page.GotoAsync("/");
        await page.ClickAsync("text=Profile");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Email", email);
        await page.ClickAsync("button:has-text('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:has-text('Continue')");

        // Should now be at the first bookend page

        var urlPath = new Uri(page.Url).LocalPath;
        Assert.EndsWith("/trn", urlPath);

        await page.ClickAsync("button:has-text('Continue')");

        // Should now be on our stub Find page

        urlPath = new Uri(page.Url).LocalPath;
        Assert.EndsWith("/FindALostTrn", urlPath);

        await page.FillAsync("#FirstName", firstName);
        await page.FillAsync("#LastName", lastName);
        await page.FillAsync("#FirstName", firstName);
        await page.FillAsync("id=DateOfBirth.Day", dateOfBirth.Day.ToString());
        await page.FillAsync("id=DateOfBirth.Month", dateOfBirth.Month.ToString());
        await page.FillAsync("id=DateOfBirth.Year", dateOfBirth.Year.ToString());
        await page.FillAsync("#Trn", trn ?? string.Empty);

        await page.ClickAsync("button:has-text('Continue')");

        // Should now be on the confirmation page

        Assert.Equal(1, await page.Locator("data-testid=first-time-user-content").CountAsync());
        await page.ClickAsync("button:has-text('Continue')");

        // Should now be back on the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        Assert.Equal(firstName, await page.InnerTextAsync("data-testid=first-name"));
        Assert.Equal(lastName, await page.InnerTextAsync("data-testid=last-name"));
        Assert.Equal(email, await page.InnerTextAsync("data-testid=email"));
        Assert.Equal(trn ?? string.Empty, await page.InnerTextAsync("data-testid=trn"));
    }
}
