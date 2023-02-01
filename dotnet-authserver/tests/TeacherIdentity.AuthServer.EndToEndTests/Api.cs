using System.Text.Json;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class Api : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public Api(HostFixture hostFixture)
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
        var apiRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users");
        apiRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _hostFixture.CapturedAccessTokens.Last());
        var apiResponse = await apiHttpClient.SendAsync(apiRequest);
        apiResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetTokenViaClientCredentialsWithReadScope_CanCallReadSupportEndpointSuccessfully()
    {
        using var httpClient = new HttpClient()
        {
            BaseAddress = new Uri(HostFixture.AuthServerBaseUrl)
        };

        // Get a token
        var token = await GetTokenViaClientCredentials(httpClient, "user:read");

        // Call API endpoint that requires user:read scope
        var apiRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users");
        apiRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var apiResponse = await httpClient.SendAsync(apiRequest);
        apiResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SignInWithUserWriteScope_CanCallWriteSupportEndpointSuccessfully()
    {
        var email = Faker.Internet.Email();
        var editingUserId = Guid.NewGuid();

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

            dbContext.Users.Add(new User()
            {
                Created = DateTime.UtcNow,
                EmailAddress = Faker.Internet.Email(),
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                UserId = editingUserId,
                UserType = UserType.Default,
                Updated = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Start on the client app and try to access a protected area with admin scope

        await page.GotoAsync($"/profile?scope={CustomScopes.UserWrite}");

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

        // Call API endpoint that requires user:write scope
        using var apiHttpClient = new HttpClient()
        {
            BaseAddress = new Uri(HostFixture.AuthServerBaseUrl)
        };
        var apiRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/{editingUserId}")
        {
            Content = JsonContent.Create(new { })
        };
        apiRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _hostFixture.CapturedAccessTokens.Last());
        var apiResponse = await apiHttpClient.SendAsync(apiRequest);
    }

    [Fact]
    public async Task GetTokenViaClientCredentialsWithWriteScope_CanCallWriteSupportEndpointSuccessfully()
    {
        var editingUserId = Guid.NewGuid();

        {
            using var scope = _hostFixture.AuthServerServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();

            dbContext.Users.Add(new User()
            {
                Created = DateTime.UtcNow,
                EmailAddress = Faker.Internet.Email(),
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                UserId = editingUserId,
                UserType = UserType.Default,
                Updated = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        using var httpClient = new HttpClient()
        {
            BaseAddress = new Uri(HostFixture.AuthServerBaseUrl)
        };

        // Get a token
        var token = await GetTokenViaClientCredentials(httpClient, "user:read");

        // Call API endpoint that requires user:read scope
        var apiRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/{editingUserId}")
        {
            Content = JsonContent.Create(new { })
        };
        apiRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var apiResponse = await httpClient.SendAsync(apiRequest);
    }

    private async Task<string> GetTokenViaClientCredentials(HttpClient httpClient, string scope)
    {
        var tokenResponse = await httpClient.PostAsync(
            "/connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "client_id", "testcc" },
                { "client_secret", "super-secret" },
                { "grant_type", "client_credentials" },
                { "scope", scope }
            }));
        tokenResponse.EnsureSuccessStatusCode();

        return (await tokenResponse.Content.ReadFromJsonAsync<JsonDocument>())!.RootElement.GetProperty("access_token").GetString()!;
    }
}
