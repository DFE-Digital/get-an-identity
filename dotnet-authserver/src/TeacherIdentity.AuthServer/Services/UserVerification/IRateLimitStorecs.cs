namespace TeacherIdentity.AuthServer.Services.UserVerification;

public interface IRateLimitStore
{
    public Task AddFailedPinVerification(string clientIp);
    public Task<bool> IsClientIpBlockedForPinVerification(string clientIp);
    public Task AddPinGeneration(string clientIp);
    public Task<bool> IsClientIpBlockedForPinGeneration(string clientIp);
}
