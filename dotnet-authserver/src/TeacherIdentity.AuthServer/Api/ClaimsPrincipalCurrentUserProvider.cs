using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Api;

public class ClaimsPrincipalCurrentUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsPrincipalCurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CurrentClientId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No current HttpContext.");
            return httpContext.User.FindFirstValue(Claims.ClientId) ?? throw new InvalidOperationException($"No '{Claims.ClientId}' claim found.");
        }
    }

    public Guid? CurrentUserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No current HttpContext.");
            return httpContext.User.GetUserId(throwIfMissing: false);
        }
    }
}
