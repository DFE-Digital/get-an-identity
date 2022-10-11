namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public interface IRateLimitStore
{
    public Task AddFailedPinVerification(string clientIp);
    public Task<bool> IsClientIpBlockedForPinVerification(string clientIp);
    public Task AddPinGeneration(string clientIp);
    public Task<bool> IsClientIpBlockedForPinGeneration(string clientIp);
}
