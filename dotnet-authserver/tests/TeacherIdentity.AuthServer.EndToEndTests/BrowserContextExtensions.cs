using Microsoft.Playwright;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public static class BrowserContextExtensions
{
    public static async Task ClearCookiesForTestClient(this IBrowserContext context)
    {
        var cookies = await context.CookiesAsync();

        await context.ClearCookiesAsync();

        // All the Auth server cookies start with 'tis-'; assume the rest are for TestClient
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
