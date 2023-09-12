using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
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

    public Task<HttpClient> CreateHttpClientWithToken(bool withUser, string? scope, TrnMatchPolicy? trnMatchPolicy = null)
    {
        var scopes = !string.IsNullOrEmpty(scope) ? new[] { scope } : Array.Empty<string>();
        return CreateHttpClientWithToken(withUser, scopes, trnMatchPolicy);
    }

    public async Task<HttpClient> CreateHttpClientWithToken(bool withUser = true, IEnumerable<string>? scopes = null, TrnMatchPolicy? trnMatchPolicy = null)
    {
        using var scope = HostFixture.Services.CreateScope();
        var userClaimHelper = scope.ServiceProvider.GetRequiredService<UserClaimHelper>();

        var userId = TestUsers.AdminUserWithAllRoles.UserId;
        var client = TestClients.DefaultClient;

        var allScopes = (withUser ? new[] { "email", "profile", "openid" } : Array.Empty<string>())
            .Concat(scopes ?? Array.Empty<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (trnMatchPolicy is null && (allScopes.Contains(CustomScopes.Trn) || allScopes.Contains(CustomScopes.DqtRead)))
        {
            trnMatchPolicy = TrnMatchPolicy.Default;
        }

        var claims = (withUser ? await userClaimHelper.GetPublicClaims(userId, trnMatchPolicy) : Array.Empty<Claim>())
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
