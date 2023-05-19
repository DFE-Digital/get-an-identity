namespace TeacherIdentity.AuthServer.Infrastructure.RateLimiting;

public class ClientIdRateLimiterOptions
{
    public required FixedWindowOptions DefaultRateLimit { get; set; } = new FixedWindowOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 300 };
    public required IDictionary<string, FixedWindowOptions> ClientRateLimits { get; set; }
}
