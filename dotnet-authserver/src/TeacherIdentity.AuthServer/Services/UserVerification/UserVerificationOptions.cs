using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.UserVerification;

public class UserVerificationOptions
{
    [Required]
    public required int PinLifetimeSeconds { get; set; }
}
