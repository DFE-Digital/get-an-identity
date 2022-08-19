using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public class EmailVerificationOptions
{
    [Required]
    public int PinLifetimeSeconds { get; set; }
}
