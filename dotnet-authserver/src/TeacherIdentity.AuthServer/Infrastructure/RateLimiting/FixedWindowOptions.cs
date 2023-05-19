namespace TeacherIdentity.AuthServer.Infrastructure.RateLimiting;

public class FixedWindowOptions
{
    public TimeSpan Window { get; set; } = TimeSpan.Zero;
    public int PermitLimit { get; set; }
}
