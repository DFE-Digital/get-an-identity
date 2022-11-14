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

        var userId = Guid.NewGuid();

        {
            using var scope = _hostFixture.AuthServerServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();

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
                CompletedTrnLookup = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Start on the client app and try to access a protected area

        await page.GotoAsync("/");
        await page.ClickAsync("text=Profile");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", email);
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
        await page.RunAndWaitForResponseAsync(
            () => page.ClickAsync("text=Sign out"),
            resp => resp.Status == 200 && resp.Url == HostFixture.ClientBaseUrl + "/");

        // Should now be back at the client, signed out

        clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        // Check events have been emitted

        _hostFixture.EventObserver.AssertEventsSaved(
            e => Assert.IsType<Events.UserSignedInEvent>(e),
            e =>
            {
                var userSignedOut = Assert.IsType<Events.UserSignedOutEvent>(e);
                Assert.Equal(DateTime.UtcNow, userSignedOut.CreatedUtc, TimeSpan.FromSeconds(10));
                Assert.Equal(userId, userSignedOut.User.UserId);
                Assert.Equal(_hostFixture.TestClientId, userSignedOut.ClientId);
            });
    }
}
