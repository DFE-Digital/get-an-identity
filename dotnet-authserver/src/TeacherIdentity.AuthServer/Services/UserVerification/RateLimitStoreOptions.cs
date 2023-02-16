using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.UserVerification;

public class RateLimitStoreOptions
{
    [Required]
    public required int PinVerificationMaxFailures { get; set; }

    [Required]
    public required int PinVerificationFailureTimeoutSeconds { get; set; }

    [Required]
    public required int PinGenerationMaxFailures { get; set; }

    [Required]
    public required int PinGenerationTimeoutSeconds { get; set; }
}
