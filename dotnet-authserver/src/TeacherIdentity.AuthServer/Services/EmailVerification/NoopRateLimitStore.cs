namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public class NoopRateLimitStore : IRateLimitStore
{
    public Task AddFailedPinVerification(string clientIp) => Task.CompletedTask;
    public Task<bool> IsClientIpBlocked(string clientIp) => Task.FromResult(false);
}
