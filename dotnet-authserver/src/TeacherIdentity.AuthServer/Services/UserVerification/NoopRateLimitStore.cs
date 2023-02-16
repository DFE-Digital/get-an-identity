namespace TeacherIdentity.AuthServer.Services.UserVerification;

public class NoopRateLimitStore : IRateLimitStore
{
    public Task AddFailedPinVerification(string clientIp) => Task.CompletedTask;
    public Task<bool> IsClientIpBlockedForPinVerification(string clientIp) => Task.FromResult(false);

    public Task AddPinGeneration(string clientIp) => Task.CompletedTask;
    public Task<bool> IsClientIpBlockedForPinGeneration(string clientIp) => Task.FromResult(false);
}
