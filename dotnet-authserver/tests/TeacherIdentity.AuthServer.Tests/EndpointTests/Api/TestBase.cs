using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;
using TeacherIdentity.AuthServer.Tests.Infrastructure;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api;

public class TestBase
{
    protected TestBase(HostFixture hostFixture)
    {
        HostFixture = hostFixture;

        ApiKeyHttpClient = hostFixture.CreateClient();
        ApiKeyHttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer test");

        HostFixture.ResetMocks();
        HostFixture.InitEventObserver();
    }

    public TestClock Clock => (TestClock)HostFixture.Services.GetRequiredService<IClock>();

    public CaptureEventObserver EventObserver => HostFixture.EventObserver;

    public HostFixture HostFixture { get; }

    public HttpClient ApiKeyHttpClient { get; }

    public Task<HttpClient> CreateHttpClientWithToken(bool withUser, string? scope)
    {
        var scopes = !string.IsNullOrEmpty(scope) ? new[] { scope } : Array.Empty<string>();
        return CreateHttpClientWithToken(withUser, scopes);
    }

    public async Task<HttpClient> CreateHttpClientWithToken(bool withUser = true, params string[] scopes)
    {
        using var scope = HostFixture.Services.CreateScope();
        var userClaimHelper = scope.ServiceProvider.GetRequiredService<UserClaimHelper>();

        var userId = TestUsers.AdminUserWithAllRoles.UserId;
        var client = TestClients.DefaultClient;

        var allScopes = (withUser ? new[] { "email", "profile", "openid" } : Array.Empty<string>())
            .Concat(scopes)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var claims = (withUser ? await userClaimHelper.GetPublicClaims(userId, hasScope: allScopes.Contains) : Array.Empty<Claim>())
            .Append(new Claim("client_id", client.ClientId!))
            .Append(new Claim(Claims.Issuer, new Uri(HostFixture.Configuration["BaseAddress"]!).AbsoluteUri))
            .Append(new Claim(Claims.Scope, string.Join(" ", allScopes)));

        var jwtHandler = new JwtSecurityTokenHandler();
        var signingCredentials = HostFixture.Services.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value.SigningCredentials.First();

        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = signingCredentials
        };

        var accessToken = jwtHandler.CreateEncodedJwt(tokenDescriptor);

        var httpClient = HostFixture.CreateClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        return httpClient;
    }

    public TestData TestData => HostFixture.Services.GetRequiredService<TestData>();
}
