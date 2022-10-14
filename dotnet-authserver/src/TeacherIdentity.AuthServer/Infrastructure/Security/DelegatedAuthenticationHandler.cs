using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace TeacherIdentity.AuthServer.Infrastructure.Security;

/// <summary>
/// An authentication handler that delegates to another for authentication and issues an event when a user is first
/// authenticated for this scheme.
/// </summary>
/// <remarks>
/// The primary reason for this to exist is so we can have different areas of the app that are authenticated by a
/// common underlying scheme (cookies) but we want to track the first time they enter each area as a distinct sign in.
/// For example, a user signs in as part of an OAuth flow then later on they access an admin area.
/// Even though the user is only authenticated once, we want to track two separate sign ins; one for the OAuth flow
/// and another for the admin area.
/// </remarks>
public class DelegatedAuthenticationHandler : IAuthenticationHandler
{
    private const string SignedInToDelegatedSchemeClaimType = "_delegated-sign-in-scheme";

    private readonly IOptionsMonitor<DelegatedAuthenticationOptions> _optionsMonitor;

    private DelegatedAuthenticationOptions _options = default!;
    private HttpContext _context = default!;
    private AuthenticationScheme _scheme = default!;

    public DelegatedAuthenticationHandler(IOptionsMonitor<DelegatedAuthenticationOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    public async Task<AuthenticateResult> AuthenticateAsync()
    {
        var delegatedResult = await _context.AuthenticateAsync(_options.DelegatedAuthenticationScheme);

        if (delegatedResult.Succeeded &&
            !delegatedResult.Principal.HasClaim(c => c.Type == SignedInToDelegatedSchemeClaimType && c.Value == _scheme.Name))
        {
            // Add a claim the indicates user has authenticated via this scheme

            var principal = delegatedResult.Principal.Clone();
            ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(SignedInToDelegatedSchemeClaimType, _scheme.Name));

            await _context.SignInAsync(_options.DelegatedAuthenticationScheme, principal);

            if (_options.OnUserSignedIn is not null)
            {
                await _options.OnUserSignedIn(_context, principal);
            }

            return AuthenticateResult.Success(new AuthenticationTicket(principal, _options.DelegatedAuthenticationScheme));
        }

        return delegatedResult;
    }

    public Task ChallengeAsync(AuthenticationProperties? properties) =>
        _context.ChallengeAsync(_options.DelegatedAuthenticationScheme, properties);

    public Task ForbidAsync(AuthenticationProperties? properties) =>
        _context.ForbidAsync(_options.DelegatedAuthenticationScheme, properties);

    public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
    {
        _scheme = scheme;
        _context = context;
        _options = _optionsMonitor.Get(_scheme.Name);

        return Task.CompletedTask;
    }
}

public class DelegatedAuthenticationOptions
{
    private string _delegatedAuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    public string DelegatedAuthenticationScheme
    {
        get => _delegatedAuthenticationScheme;
        set => _delegatedAuthenticationScheme = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Func<HttpContext, ClaimsPrincipal, Task>? OnUserSignedIn { get; set; }
}
