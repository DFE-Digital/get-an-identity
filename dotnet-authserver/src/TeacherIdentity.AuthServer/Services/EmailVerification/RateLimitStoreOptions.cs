using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public class RateLimitStoreOptions
{
    [Required]
    public required int MaxFailures { get; set; }

    [Required]
    public required int FailureTimeoutSeconds { get; set; }
}
