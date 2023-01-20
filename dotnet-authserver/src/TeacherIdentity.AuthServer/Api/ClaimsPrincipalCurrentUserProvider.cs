namespace TeacherIdentity.AuthServer.Api;

public class ClaimsPrincipalCurrentUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsPrincipalCurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid CurrentUserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No current HttpContext.");
            return httpContext.User.GetUserId()!.Value;
        }
    }
}
