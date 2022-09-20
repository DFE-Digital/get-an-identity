using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class SignOut : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public SignOut(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
    }

    [Fact]
    public async Task User_CanSignOut()
    {
        var email = "joe.bloggs+existing-user@example.com";
        var trn = "1234567";

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
                UserType = UserType.Teacher,
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                Trn = trn
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

        // Hit the sign out link
        await page.ClickAsync("text=Sign out");

        // Should now be back at the client, signed out

        clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);
    }
}
