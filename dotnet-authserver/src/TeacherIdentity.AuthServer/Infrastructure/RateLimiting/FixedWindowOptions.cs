namespace TeacherIdentity.AuthServer.Infrastructure.RateLimiting;

public class FixedWindowOptions
{
    public required TimeSpan Window { get; set; }
    public required int PermitLimit { get; set; }
}
