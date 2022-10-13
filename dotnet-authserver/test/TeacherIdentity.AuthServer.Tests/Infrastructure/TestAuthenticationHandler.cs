using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly CurrentUserIdContainer _currentUserIdContainer;

    public TestAuthenticationHandler(
        CurrentUserIdContainer currentUserIdContainer,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
        _currentUserIdContainer = currentUserIdContainer;
    }

    protected override Task InitializeHandlerAsync()
    {
        Options.ForwardChallenge = CookieAuthenticationDefaults.AuthenticationScheme;

        return base.InitializeHandlerAsync();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (_currentUserIdContainer.CurrentUserId.Value is Guid userId)
        {
            using var dbContext = Context.RequestServices.GetRequiredService<TeacherIdentityServerDbContext>();
            var user = await dbContext.Users.SingleAsync(u => u.UserId == userId);

            var claims = UserClaimHelper.GetInternalClaims(user);
            var principal = AuthenticationState.CreatePrincipal(claims);

            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }

        return AuthenticateResult.NoResult();
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        // Allow the cookies handler to own handle this

        return Task.CompletedTask;
    }
}

public class CurrentUserIdContainer
{
    public AsyncLocal<Guid?> CurrentUserId { get; } = new();
}
