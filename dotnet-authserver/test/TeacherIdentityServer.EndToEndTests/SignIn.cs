using TeacherIdentityServer.Models;

namespace TeacherIdentityServer.EndToEndTests;

public class SignIn : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public SignIn(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
    }

    [Fact]
    public async Task ExistingUser_CanSignInSuccessfullyWithEmailAndPin()
    {
        var email = "joe.bloggs@example.com";

        {
            using var scope = _hostFixture.AuthServerServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();

            dbContext.Users.Add(new TeacherIdentityUser()
            {
                EmailAddress = email,
                FirstName = "Joe",
                LastName = "Bloggs",
                UserId = Guid.NewGuid(),
                Trn = "1234567"
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
        await page.ClickAsync("text=Continue");

        await page.FillAsync("text=Enter your code", "123456");
        await page.ClickAsync("text=Continue");

        // Should now be back at the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        var signedInEmail = await page.InnerTextAsync("data-testid=email");
        Assert.Equal(email, signedInEmail);
    }
}
