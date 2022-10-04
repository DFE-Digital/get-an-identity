namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public interface IRateLimitStore
{
    public Task AddFailedPinVerification(string clientIp);
    public Task<bool> IsClientIpBlocked(string clientIp);
}
