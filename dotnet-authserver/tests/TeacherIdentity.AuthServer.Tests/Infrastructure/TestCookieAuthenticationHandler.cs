using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class TestCookieAuthenticationHandler : CookieAuthenticationHandler
{
    private readonly CurrentUserIdContainer _currentUserIdContainer;

    public TestCookieAuthenticationHandler(
        IOptionsMonitor<CookieAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        CurrentUserIdContainer currentUserIdContainer) : base(options, logger, encoder)
    {
        _currentUserIdContainer = currentUserIdContainer;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (_currentUserIdContainer.CurrentUserId.Value is Guid userId)
        {
            using var dbContext = Context.RequestServices.GetRequiredService<TeacherIdentityServerDbContext>();
            var user = await dbContext.Users.SingleAsync(u => u.UserId == userId);

            var claims = UserClaimHelper.GetInternalClaims(user);
            var principal = AuthenticationState.CreatePrincipal(claims);
            var properties = new AuthenticationProperties()
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10)
            };

            return AuthenticateResult.Success(new AuthenticationTicket(principal, properties, Scheme.Name));
        }

        return await base.HandleAuthenticateAsync();
    }
}

public class CurrentUserIdContainer
{
    public AsyncLocal<Guid?> CurrentUserId { get; } = new();
}
