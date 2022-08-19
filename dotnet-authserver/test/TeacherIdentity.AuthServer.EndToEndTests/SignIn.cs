using FakeItEasy;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

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
    public async Task ExistingUser_CanSignInSuccessfullyWithEmailAndPin()
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
            });

            await dbContext.SaveChangesAsync();

            A.CallTo(() => _hostFixture.DqtApiClient.GetTeacherIdentityInfo(userId))
                .Returns(new DqtTeacherIdentityInfo() { Trn = trn, UserId = userId });
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

        await page.ClickAsync("button:has-text('Continue')");

        // Should now be back at the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        var signedInEmail = await page.InnerTextAsync("data-testid=email");
        Assert.Equal(trn, await page.InnerTextAsync("data-testid=trn"));
        Assert.Equal(email, signedInEmail);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NewUser_IsRedirectedToFindAndRegistersAnAccountOnCallback(bool hasTrn)
    {
        var email = $"joe.bloggs+new-user-{(hasTrn ? "with-trn" : "without-trn")}@example.com";
        var firstName = "Joe";
        var lastName = "Bloggs";
        var trn = hasTrn ? "2345678" : null;
        var dateOfBirth = new DateOnly(1990, 1, 2);

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

        // Should now be on the confirmation page

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
