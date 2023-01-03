using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class SupportApi : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public SupportApi(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
    }

    [Fact]
    public async Task SignInWithUserReadScope_CanCallReadSupportEndpointSuccessfully()
    {
        var email = Faker.Internet.Email();

        {
            using var scope = _hostFixture.AuthServerServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();

            dbContext.Users.Add(new User()
            {
                Created = DateTime.UtcNow,
                EmailAddress = email,
                FirstName = "Joe",
                LastName = "Bloggs",
                UserId = Guid.NewGuid(),
                UserType = UserType.Staff,
                StaffRoles = new[] { StaffRoles.GetAnIdentityAdmin },
                Updated = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Start on the client app and try to access a protected area with admin scope

        await page.GotoAsync($"/profile?scope={CustomScopes.UserRead}");

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

        // Call API endpoint that requires user:read scope
        using var apiHttpClient = new HttpClient()
        {
            BaseAddress = new Uri(HostFixture.AuthServerBaseUrl)
        };
        apiHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _hostFixture.CapturedAccessTokens.Last());
        var apiResponse = await apiHttpClient.GetAsync("/api/v1/users");
        apiResponse.EnsureSuccessStatusCode();
    }
}
