using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Services.UserVerification;

public class FixedPinUserVerificationService : IUserVerificationService
{
    private readonly FixedPinUserVerificationOptions _options;

    public FixedPinUserVerificationService(IOptions<FixedPinUserVerificationOptions> optionsAccessor)
    {
        _options = optionsAccessor.Value;
    }

    public Task<PinGenerationResult> GenerateEmailPin(string email) =>
        Task.FromResult(PinGenerationResult.Success(_options.Pin));

    public Task<PinGenerationResult> GenerateSmsPin(MobileNumber mobileNumber) =>
        Task.FromResult(PinGenerationResult.Success(_options.Pin));

    public Task<PinVerificationFailedReasons> VerifyEmailPin(string email, string pin)
    {
        var result = pin == _options.Pin ? PinVerificationFailedReasons.None : PinVerificationFailedReasons.Unknown;
        return Task.FromResult(result);
    }

    public Task<PinVerificationFailedReasons> VerifySmsPin(MobileNumber mobileNumber, string pin)
    {
        var result = pin == _options.Pin ? PinVerificationFailedReasons.None : PinVerificationFailedReasons.Unknown;
        return Task.FromResult(result);
    }
}
