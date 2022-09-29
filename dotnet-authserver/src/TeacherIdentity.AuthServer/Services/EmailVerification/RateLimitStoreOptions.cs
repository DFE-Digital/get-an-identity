using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public class RateLimitStoreOptions
{
    [Required]
    public int MaxFailures { get; set; }
    [Required]
    public int FailureTimeoutSeconds { get; set; }
}
